using System.Security.Cryptography;
using System.Text;

namespace JSAPNEW.Services.Implementation
{
    [Obsolete("Legacy encryption for migration only. Use PasswordHasher for new passwords.")]
    public static class Encryption
    {
        private static readonly string _legacyKey = "prhfialkmcn";
        private static readonly byte[] _legacySalt = { 0x43, 0x87, 0x23, 0x72, 0x45, 0x56, 0x68, 0x14, 0x62, 0x84 };

        public static string Encrypt(string input)
        {
            return Convert.ToBase64String(EncryptBytes(Encoding.UTF8.GetBytes(input)));
        }

        public static byte[] EncryptBytes(byte[] input)
        {
#pragma warning disable SYSLIB0023
            var pdb = new PasswordDeriveBytes(_legacyKey, _legacySalt);
#pragma warning restore SYSLIB0023
            using var ms = new MemoryStream();
            using var aes = new AesManaged();
            aes.Key = pdb.GetBytes(aes.KeySize / 8);
            aes.IV = pdb.GetBytes(aes.BlockSize / 8);
            using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(input, 0, input.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }

        public static string Decrypt(string input)
        {
            return Encoding.UTF8.GetString(DecryptBytes(Convert.FromBase64String(input)));
        }

        public static byte[] DecryptBytes(byte[] input)
        {
#pragma warning disable SYSLIB0023
            var pdb = new PasswordDeriveBytes(_legacyKey, _legacySalt);
#pragma warning restore SYSLIB0023
            using var ms = new MemoryStream();
            using var aes = new AesManaged();
            aes.Key = pdb.GetBytes(aes.KeySize / 8);
            aes.IV = pdb.GetBytes(aes.BlockSize / 8);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(input, 0, input.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }

        public static bool IsLegacyEncrypted(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length < 8) return false;
            try
            {
                var bytes = Convert.FromBase64String(input);
                var decrypted = DecryptBytes(bytes);
                return decrypted.Length > 0 && decrypted.All(b => b >= 32 && b <= 126);
            }
            catch { return false; }
        }
    }
}
