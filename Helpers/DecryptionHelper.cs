using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using WechatPCMsgBakTool.Model;

namespace WechatPCMsgBakTool.Helpers
{
    public class DecryptionHelper
    {
        const int IV_SIZE = 16;
        const int HMAC_SHA1_SIZE = 20;
        const int KEY_SIZE = 32;
        const int AES_BLOCK_SIZE = 16;
        const int DEFAULT_ITER = 64000;
        const int DEFAULT_PAGESIZE = 4096; //4048数据 + 16IV + 20 HMAC + 12
        const string SQLITE_HEADER = "SQLite format 3";
        public static byte[]? GetWechatKey(bool mem_find_key,string account)
        {
            Process? process = ProcessHelper.GetProcess("WeChat");
            if (process == null)
            {
                return null;
            }
            ProcessModule? module = ProcessHelper.FindProcessModule(process.Id, "WeChatWin.dll");
            if (module == null)
            {
                return null;
            }
            string? version = module.FileVersionInfo.FileVersion;
            if (version == null)
            {
                return null;
            }

            

            if (!mem_find_key)
            {
                List<VersionInfo>? info = null;
                string json = File.ReadAllText("version.json");
                info = JsonConvert.DeserializeObject<List<VersionInfo>?>(json);
                if (info == null)
                    return null;
                if (info.Count == 0)
                    return null;

                VersionInfo? cur = info.Find(x => x.Version == version);
                if (cur == null)
                    return null;
                //这里加的是版本偏移量，兼容不同版本把这个加给改了
                long baseAddress = (long)module.BaseAddress + cur.BaseAddr;
                byte[]? bytes = ProcessHelper.ReadMemoryDate(process.Handle, (IntPtr)baseAddress, 8);
                if (bytes != null)
                {
                    IntPtr baseAddress2 = (IntPtr)(((long)bytes[7] << 56) + ((long)bytes[6] << 48) + ((long)bytes[5] << 40) + ((long)bytes[4] << 32) + ((long)bytes[3] << 24) + ((long)bytes[2] << 16) + ((long)bytes[1] << 8) + (long)bytes[0]);
                    byte[]? twoGet = ProcessHelper.ReadMemoryDate(process.Handle, baseAddress2, 32);
                    if (twoGet != null)
                    {
                        string key = BytesToHex(twoGet);
                        return twoGet;
                    }
                }
            }
            else
            {
                List<int> read = ProcessHelper.FindProcessMemory(process.Handle, module, account);
                if(read.Count >= 2)
                {
                    byte[] buffer = new byte[8];
                    int key_offset = read[1] - 64;
                    if (ProcessHelper.ReadProcessMemory(process.Handle, module.BaseAddress + key_offset, buffer, buffer.Length, out _))
                    {
                        ulong addr = BitConverter.ToUInt64(buffer, 0);

                        byte[] key_bytes = new byte[32];
                        if(ProcessHelper.ReadProcessMemory(process.Handle, (IntPtr)addr, key_bytes, key_bytes.Length, out _))
                        {
                            return key_bytes;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("搜索不到微信账号，请确认用户名是否正确，如错误请重新新建工作区，务必确认账号是否正确", "错误");
                }
            }
            return null;
        }
        public static byte[] DecryptDB(byte[] db_file_bytes, byte[] password_bytes)
        {
            //数据库头16字节是盐值
            var salt = db_file_bytes.Take(16).ToArray();
            //HMAC验证时用的盐值需要亦或0x3a
            byte[] hmac_salt = new byte[16];
            for (int i = 0; i < salt.Length; i++)
            {
                hmac_salt[i] = (byte)(salt[i] ^ 0x3a);
            }
            //计算保留段长度
            int reserved = IV_SIZE;
            reserved += HMAC_SHA1_SIZE;
            reserved = ((reserved % AES_BLOCK_SIZE) == 0) ? reserved : ((reserved / AES_BLOCK_SIZE) + 1) * AES_BLOCK_SIZE;

            //密钥扩展，分别对应AES解密密钥和HMAC验证密钥
            byte[] key = new byte[KEY_SIZE];
            byte[] hmac_key = new byte[KEY_SIZE];
            OpenSSLInterop.PKCS5_PBKDF2_HMAC_SHA1(password_bytes, password_bytes.Length, salt, salt.Length, DEFAULT_ITER, key.Length, key);
            OpenSSLInterop.PKCS5_PBKDF2_HMAC_SHA1(key, key.Length, hmac_salt, hmac_salt.Length, 2, hmac_key.Length, hmac_key);

            int page_no = 0;
            int offset = 16;
            Console.WriteLine("开始解密...");
            var hmac_sha1 = HMAC.Create("HMACSHA1");
            hmac_sha1!.Key = hmac_key;
            List<byte> decrypted_file_bytes = new List<byte>();
            while (page_no < db_file_bytes.Length / DEFAULT_PAGESIZE)
            {
                byte[] decryped_page_bytes = new byte[DEFAULT_PAGESIZE];
                byte[] going_to_hashed = new byte[DEFAULT_PAGESIZE - reserved - offset + IV_SIZE + 4];
                db_file_bytes.Skip((page_no * DEFAULT_PAGESIZE) + offset).Take(DEFAULT_PAGESIZE - reserved - offset + IV_SIZE).ToArray().CopyTo(going_to_hashed, 0);
                var page_bytes = BitConverter.GetBytes(page_no + 1);
                page_bytes.CopyTo(going_to_hashed, DEFAULT_PAGESIZE - reserved - offset + IV_SIZE);
                //计算分页的Hash
                var hash_mac_compute = hmac_sha1.ComputeHash(going_to_hashed, 0, going_to_hashed.Count());
                //取出分页中存储的Hash
                var hash_mac_cached = db_file_bytes.Skip((page_no * DEFAULT_PAGESIZE) + DEFAULT_PAGESIZE - reserved + IV_SIZE).Take(hash_mac_compute.Length).ToArray();
                //对比两个Hash
                if (!hash_mac_compute.SequenceEqual(hash_mac_cached))
                {
                    Console.WriteLine("Hash错误...");
                    return decrypted_file_bytes.ToArray();
                }
                else
                {
                    Console.WriteLine($"解密第[{page_no + 1}]页");
                    if (page_no == 0)
                    {
                        var header_bytes = Encoding.ASCII.GetBytes(SQLITE_HEADER);
                        header_bytes.CopyTo(decryped_page_bytes, 0);
                    }
                    var encrypted_content = db_file_bytes.Skip((page_no * DEFAULT_PAGESIZE) + offset).Take(DEFAULT_PAGESIZE - reserved - offset).ToArray();
                    var iv = db_file_bytes.Skip((page_no * DEFAULT_PAGESIZE) + (DEFAULT_PAGESIZE - reserved)).Take(16).ToArray();
                    var decrypted_content = DecryptionHelper.AESDecrypt(encrypted_content, key, iv);
                    decrypted_content.CopyTo(decryped_page_bytes, offset);
                    var reserved_bytes = db_file_bytes.Skip((page_no * DEFAULT_PAGESIZE) + DEFAULT_PAGESIZE - reserved).Take(reserved).ToArray();
                    reserved_bytes.CopyTo(decryped_page_bytes, DEFAULT_PAGESIZE - reserved);
                }
                page_no++;
                offset = 0;
                foreach (var item in decryped_page_bytes)
                {
                    decrypted_file_bytes.Add(item);
                }
            }
            return decrypted_file_bytes.ToArray();
        }
        public static byte[] AESDecrypt(byte[] content, byte[] key, byte[] iv)
        {
            Aes rijndaelCipher = Aes.Create();
            rijndaelCipher.Mode = CipherMode.CBC;
            rijndaelCipher.Padding = PaddingMode.None;
            rijndaelCipher.KeySize = 256;
            rijndaelCipher.BlockSize = 128;
            rijndaelCipher.Key = key;
            rijndaelCipher.IV = iv;
            ICryptoTransform transform = rijndaelCipher.CreateDecryptor();
            byte[] plain_bytes = transform.TransformFinalBlock(content, 0, content.Length);
            return plain_bytes;
        }
        private static string BytesToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes, 0).Replace("-", string.Empty).ToLower().ToUpper();
        }
        public static byte[] DecImage(string source)
        {
            //读取数据
            byte[] fileBytes = File.ReadAllBytes(source);
            //算差异转换
            byte key = GetImgKey(fileBytes);
            fileBytes = ConvertData(fileBytes, key);
            return fileBytes;
        }
        public static string CheckFileType(byte[] data)
        {
            switch (data[0])
            {
                case 0XFF:  //byte[] jpg = new byte[] { 0xFF, 0xD8, 0xFF };
                    {
                        if (data[1] == 0xD8 && data[2] == 0xFF)
                        {
                            return ".jpg";
                        }
                        break;
                    }
                case 0x89:  //byte[] png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                    {
                        if (data[1] == 0x50 && data[2] == 0x4E && data[7] == 0x0A)
                        {
                            return ".png";
                        }
                        break;
                    }
                case 0x42:  //byte[] bmp = new byte[] { 0x42, 0x4D };
                    {
                        if (data[1] == 0X4D)
                        {
                            return ".bmp";
                        }
                        break;
                    }
                case 0x47:  //byte[] gif = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39(0x37), 0x61 };
                    {
                        if (data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x38 && data[5] == 0x61)
                        {
                            return ".gif";
                        }
                        break;
                    }
                case 0x49:  // byte[] tif = new byte[] { 0x49, 0x49, 0x2A, 0x00 };
                    {
                        if (data[1] == 0x49 && data[2] == 0x2A && data[3] == 0x00)
                        {
                            return ".tif";
                        }
                        break;
                    }
                case 0x4D:  //byte[] tif = new byte[] { 0x4D, 0x4D, 0x2A, 0x00 };
                    {
                        if (data[1] == 0x4D && data[2] == 0x2A && data[3] == 0x00)
                        {
                            return ".tif";
                        }
                        break;
                    }
            }

            return ".dat";
        }
        private static byte GetImgKey(byte[] fileRaw)
        {
            byte[] raw = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                raw[i] = fileRaw[i];
            }

            for (byte key = 0x01; key < 0xFF; key++)
            {
                byte[] buf = new byte[8];
                raw.CopyTo(buf, 0);

                if (CheckFileType(ConvertData(buf, key)) != ".dat")
                {
                    return key;
                }
            }
            return 0x00;
        }
        private static byte[] ConvertData(byte[] data, byte key)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= key;
            }

            return data;
        }
        public static string SaveDecImage(byte[] fileRaw,string source,string to_dir,string type)
        {
            FileInfo fileInfo = new FileInfo(source);
            string fileName = fileInfo.Name.Substring(0, fileInfo.Name.Length - 4);
            string saveFilePath = Path.Combine(to_dir, fileName + type);
            using (FileStream fileStream = File.OpenWrite(saveFilePath))
            {
                fileStream.Write(fileRaw, 0, fileRaw.Length);
                fileStream.Flush();
            }
            return saveFilePath;
        }
    }

}
