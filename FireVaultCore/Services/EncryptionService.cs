using System;
using System.Security.Cryptography;
using System.Text;

namespace FireVaultCore.Services
{
    internal static class EncryptionService
    {
        public static string Encrypt(string plainText, string salt)
        {
            using (var aes = Aes.Create())
            {
                var key = DeriveKey(salt);
                aes.Key = key;
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new System.IO.MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new System.IO.StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherText, string salt)
        {
            using (var aes = Aes.Create())
            {
                var key = DeriveKey(salt);
                aes.Key = key;

                var fullCipher = Convert.FromBase64String(cipherText);
                var iv = new byte[aes.IV.Length];
                var cipher = new byte[fullCipher.Length - iv.Length];

                Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

                using (var decryptor = aes.CreateDecryptor(aes.Key, iv))
                using (var ms = new System.IO.MemoryStream(cipher))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new System.IO.StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        public static string HashPassword(string password, string salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt), 10000, HashAlgorithmName.SHA256))
            {
                return Convert.ToBase64String(pbkdf2.GetBytes(32));
            }
        }

        private static byte[] DeriveKey(string salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(salt, Encoding.UTF8.GetBytes(salt), 10000, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(32);
            }
        }
    }
}

