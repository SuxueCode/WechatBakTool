using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WechatBakTool.Helpers
{
    public class OpenSSLInterop
    {
        private const string Lib = "libcrypto-1_1";
        internal static unsafe int HMAC_Init(out HMAC_CTX ctx, byte[] key, int key_len, IntPtr md)
        {
            return HMAC_InitNative(out ctx, key, key_len, md);
        }

        internal static void HMAC_Init_ex(ref HMAC_CTX ctx, byte[] key, int key_len, IntPtr md, IntPtr zero)
        {
            HMAC_Init_exNative(ref ctx, key, key_len, md, zero);
        }

        internal static unsafe int HMAC_Update(ref HMAC_CTX ctx, byte* data, int len)
        {
            return HMAC_UpdateNative(ref ctx, data, len);
        }

        internal static unsafe int HMAC_Final(ref HMAC_CTX ctx, byte* md, ref uint len)
        {
            return HMAC_FinalNative(ref ctx, md, ref len);
        }

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public extern static int PKCS5_PBKDF2_HMAC_SHA1(byte[] pass, int passlen, byte[] salt, int saltlen, int iter, int keylen, byte[] outBytes);
        [DllImport(Lib, EntryPoint = "HMAC_Init", ExactSpelling = true)]
        private extern static unsafe int HMAC_InitNative(out HMAC_CTX ctx, byte[] key, int key_len, IntPtr md);

        [DllImport(Lib, EntryPoint = "HMAC_Init_ex", ExactSpelling = true)]
        private extern static void HMAC_Init_exNative(ref HMAC_CTX ctx, byte[] key, int key_len, IntPtr md, IntPtr zero);

        [DllImport(Lib, EntryPoint = "HMAC_Update", ExactSpelling = true)]
        private extern static unsafe int HMAC_UpdateNative(ref HMAC_CTX ctx, byte* data, int len);

        [DllImport(Lib, EntryPoint = "HMAC_Final", ExactSpelling = true)]
        private extern static unsafe int HMAC_FinalNative(ref HMAC_CTX ctx, byte* md, ref uint len);

        [DllImport(Lib)]
        internal extern static unsafe void HMAC_CTX_cleanup(ref HMAC_CTX ctx);

        [StructLayout(LayoutKind.Explicit, Size = 512)]
        internal struct HMAC_CTX { }
    }
}
