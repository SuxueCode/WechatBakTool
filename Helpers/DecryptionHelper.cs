using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using WechatBakTool.Model;
using WechatBakTool.Pages;
using WechatBakTool.ViewModel;

namespace WechatBakTool.Helpers
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
        public static byte[]? GetWechatKey(string pid, int find_key_type, string account)
        {
            Process process = Process.GetProcessById(int.Parse(pid));
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

            if (find_key_type == 1)
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
            else if(find_key_type == 2)
            {
                List<int> read = ProcessHelper.FindProcessMemory(process.Handle, module, account);
                if (read.Count >= 2)
                {
                    byte[] buffer = new byte[8];
                    int key_offset = read[1] - 64;
                    if (NativeAPI.ReadProcessMemory(process.Handle, module.BaseAddress + key_offset, buffer, buffer.Length, out _))
                    {
                        ulong addr = BitConverter.ToUInt64(buffer, 0);

                        byte[] key_bytes = new byte[32];
                        if (NativeAPI.ReadProcessMemory(process.Handle, (IntPtr)addr, key_bytes, key_bytes.Length, out _))
                        {
                            return key_bytes;
                        }
                    }
                }
            }
            else if (find_key_type == 3)
            {
                string searchString = "-----BEGIN PUBLIC KEY-----";
                List<long> addr = NativeAPIHelper.SearchProcessAllMemory(process, searchString);
                if (addr.Count > 0)
                {
                    foreach (long a in addr)
                    {
                        byte[] buffer = new byte[module.ModuleMemorySize];
                        byte[] search = BitConverter.GetBytes(a);
                        Array.Resize(ref search, 8);
                        int read = 0;

                        List<int> offset = new List<int>();
                        if (NativeAPI.ReadProcessMemory(process.Handle, module.BaseAddress, buffer, buffer.Length, out read))
                        {
                            for (int i = 0; i < buffer.Length - 1; i++)
                            {
                                if (buffer[i] == search[0])
                                {
                                    for (int s = 1; s < search.Length; s++)
                                    {
                                        if (buffer[i + s] != search[s])
                                            break;
                                        if (s == search.Length - 1)
                                        {
                                            long iii = (long)module.BaseAddress + i - 0xd8;

                                            byte[] key = new byte[8];
                                            if (NativeAPI.ReadProcessMemory(process.Handle, new IntPtr(iii), key, key.Length, out _))
                                            {
                                                ulong key_addr = BitConverter.ToUInt64(key, 0);

                                                byte[] key_bytes = new byte[32];
                                                NativeAPI.ReadProcessMemory(process.Handle, (IntPtr)key_addr, key_bytes, key_bytes.Length, out _);
                                                string key1 = BitConverter.ToString(key_bytes, 0);
                                                return key_bytes;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception("搜索不到微信账号，请确认用户名是否正确，如错误请重新新建工作区，务必确认账号是否正确");
                }
            }
            else if (find_key_type == 3)
            {
                string searchString = "-----BEGIN PUBLIC KEY-----";
            }
            return null;
        }

        public static void DecryptDB(string file, string to_file, byte[] password_bytes)
        {
            //数据库头16字节是盐值
            byte[] salt_key = new byte[16];

            FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
            fileStream.Read(salt_key, 0, 16);

            //HMAC验证时用的盐值需要亦或0x3a
            byte[] hmac_salt = new byte[16];
            for (int i = 0; i < salt_key.Length; i++)
            {
                hmac_salt[i] = (byte)(salt_key[i] ^ 0x3a);
            }
            //计算保留段长度
            int reserved = IV_SIZE;
            reserved += HMAC_SHA1_SIZE;
            reserved = ((reserved % AES_BLOCK_SIZE) == 0) ? reserved : ((reserved / AES_BLOCK_SIZE) + 1) * AES_BLOCK_SIZE;

            //密钥扩展，分别对应AES解密密钥和HMAC验证密钥
            byte[] key = new byte[KEY_SIZE];
            byte[] hmac_key = new byte[KEY_SIZE];
            OpenSSLInterop.PKCS5_PBKDF2_HMAC_SHA1(password_bytes, password_bytes.Length, salt_key, salt_key.Length, DEFAULT_ITER, key.Length, key);
            OpenSSLInterop.PKCS5_PBKDF2_HMAC_SHA1(key, key.Length, hmac_salt, hmac_salt.Length, 2, hmac_key.Length, hmac_key);

            int page_no = 0;
            int offset = 16;
            Console.WriteLine("开始解密...");
            var hmac_sha1 = HMAC.Create("HMACSHA1");
            hmac_sha1!.Key = hmac_key;

            List<byte> decrypted_file_bytes = new List<byte>();
            FileStream tofileStream = new FileStream(to_file, FileMode.OpenOrCreate, FileAccess.Write);

            using (fileStream)
            {
                try
                {
                    // 当前分页小于计算分页数
                    while (page_no < fileStream.Length / DEFAULT_PAGESIZE)
                    {
                        // 读内容
                        byte[] decryped_page_bytes = new byte[DEFAULT_PAGESIZE];
                        byte[] going_to_hashed = new byte[DEFAULT_PAGESIZE - reserved - offset + IV_SIZE + 4];
                        fileStream.Seek((page_no * DEFAULT_PAGESIZE) + offset, SeekOrigin.Begin);
                        fileStream.Read(going_to_hashed, 0, DEFAULT_PAGESIZE - reserved - offset + IV_SIZE);

                        // 分页标志
                        var page_bytes = BitConverter.GetBytes(page_no + 1);
                        page_bytes.CopyTo(going_to_hashed, DEFAULT_PAGESIZE - reserved - offset + IV_SIZE);
                        var hash_mac_compute = hmac_sha1.ComputeHash(going_to_hashed, 0, going_to_hashed.Length);

                        // 取分页hash
                        byte[] hash_mac_cached = new byte[hash_mac_compute.Length];
                        fileStream.Seek((page_no * DEFAULT_PAGESIZE) + DEFAULT_PAGESIZE - reserved + IV_SIZE, SeekOrigin.Begin);
                        fileStream.Read(hash_mac_cached, 0, hash_mac_compute.Length);

                        if (!hash_mac_compute.SequenceEqual(hash_mac_cached) && page_no == 0)
                        {
                            Console.WriteLine("Hash错误...");
                            return;
                        }
                        else
                        {
                            if (page_no == 0)
                            {
                                var header_bytes = Encoding.ASCII.GetBytes(SQLITE_HEADER);
                                header_bytes.CopyTo(decryped_page_bytes, 0);
                            }

                            // 加密内容
                            byte[] page_content = new byte[DEFAULT_PAGESIZE - reserved - offset];
                            fileStream.Seek((page_no * DEFAULT_PAGESIZE) + offset, SeekOrigin.Begin);
                            fileStream.Read(page_content, 0, DEFAULT_PAGESIZE - reserved - offset);

                            // iv
                            byte[] iv = new byte[16];
                            fileStream.Seek((page_no * DEFAULT_PAGESIZE) + (DEFAULT_PAGESIZE - reserved), SeekOrigin.Begin);
                            fileStream.Read(iv, 0, 16);

                            var decrypted_content = AESDecrypt(page_content, key, iv);
                            decrypted_content.CopyTo(decryped_page_bytes, offset);

                            // 保留
                            byte[] reserved_byte = new byte[reserved];
                            fileStream.Seek((page_no * DEFAULT_PAGESIZE) + DEFAULT_PAGESIZE - reserved, SeekOrigin.Begin);
                            fileStream.Read(reserved_byte, 0, reserved);
                            reserved_byte.CopyTo(decryped_page_bytes, DEFAULT_PAGESIZE - reserved);

                            tofileStream.Write(decryped_page_bytes, 0, decryped_page_bytes.Length);

                        }
                        page_no++;
                        offset = 0;
                    }
                }catch(Exception ex)
                {
                    File.AppendAllText("err.log", "page=>" + page_no.ToString() + "\r\n");
                    File.AppendAllText("err.log", "size=>" + fileStream.Length.ToString() + "\r\n");
                    File.AppendAllText("err.log", "postion=>" + ((page_no * DEFAULT_PAGESIZE) + offset).ToString() + "\r\n");
                    File.AppendAllText("err.log", ex.ToString() + "\r\n");
                }
            }
            /*
             * 旧版解密
            while (page_no < fileStream.Length / DEFAULT_PAGESIZE)
            {
                byte[] decryped_page_bytes = new byte[DEFAULT_PAGESIZE];
                byte[] going_to_hashed = new byte[DEFAULT_PAGESIZE - reserved - offset + IV_SIZE + 4];
                db_file_bytes.Skip((page_no * DEFAULT_PAGESIZE) + offset).Take(DEFAULT_PAGESIZE - reserved - offset + IV_SIZE).ToArray().CopyTo(going_to_hashed, 0);
                var page_bytes = BitConverter.GetBytes(page_no + 1);
                page_bytes.CopyTo(going_to_hashed, DEFAULT_PAGESIZE - reserved - offset + IV_SIZE);
                //计算分页的Hash
                var hash_mac_compute = hmac_sha1.ComputeHash(going_to_hashed, 0, going_to_hashed.Length);
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
            }*/
            tofileStream.Close();
            tofileStream.Dispose();
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

        private readonly static List<byte[]> ImgHeader = new List<byte[]>()
        {
            new byte[] { 0xFF, 0xD8 },//JPG
            new byte[] { 0x89, 0x50 },//PNG
            new byte[] { 0x42, 0x4D },//BMP
            new byte[] { 0x47, 0x49 },//GIF
            new byte[] { 0x49, 0x49 },//TIF
            new byte[] { 0x4D, 0x4D },//TIF
        };
        public static byte[] DecImage(string source)
        {
            //读取数据
            byte[] fileBytes = File.ReadAllBytes(source);
            //算差异转换
            foreach (byte[] b in ImgHeader)
            {
                byte t = (byte)(fileBytes[0] ^ b[0]);
                byte[] decData = fileBytes.Select(b => (byte)(b ^ t)).ToArray();
                if (b[1] != decData[1])
                    continue;
                else
                {
                    return decData;
                }
            }
            return new byte[0];
        }
        public static string CheckFileType(byte[] data)
        {
            if (data[0] == 0xFF && data[1] == 0xD8)
                return ".jpg";
            else if (data[0] == 0x89 && data[1] == 0x50)
                return ".png";
            else if (data[0] == 0x42 && data[1] == 0X4D)
                return ".bmp";
            else if (data[0] == 0x47 && data[1] == 0x49)
                return ".gif";
            else if (data[0] == 0x49 && data[1] == 0x49)
                return ".tif";
            else if (data[0] == 0x4D && data[1] == 0x4D)
                return ".tif";
            else
                return ".dat";
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
        public static void DecryUserData(byte[] key, string source, string to,CreateWorkViewModel viewModel)
        {
            string dbPath = source;
            string decPath = to;
            if (!Directory.Exists(decPath))
                Directory.CreateDirectory(decPath);

            string[] filePath = Directory.GetFiles(dbPath);
            foreach (string file in filePath)
            {
                FileInfo info = new FileInfo(file);
                viewModel.LabelStatus = "正在解密" + info.Name;
                string to_file = Path.Combine(decPath, info.Name);
                DecryptDB(file,to_file, key);
            }
        }
    }

}
