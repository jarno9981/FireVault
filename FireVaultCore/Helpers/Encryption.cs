using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FireVaultCore.Helpers
{
    public static class Encryption
    {
        public static string Encrypt(string plainText, string password)
        {
            byte[] salt = GenerateRandomSalt();
            byte[] key = GenerateKey(password, salt);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                byte[] iv = aes.IV;

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    memoryStream.Write(salt, 0, salt.Length);
                    memoryStream.Write(iv, 0, iv.Length);

                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                    {
                        streamWriter.Write(plainText);
                    }

                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherText, string password)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            byte[] salt = new byte[16];
            byte[] iv = new byte[16];
            byte[] encryptedData = new byte[cipherBytes.Length - 32];

            Buffer.BlockCopy(cipherBytes, 0, salt, 0, 16);
            Buffer.BlockCopy(cipherBytes, 16, iv, 0, 16);
            Buffer.BlockCopy(cipherBytes, 32, encryptedData, 0, encryptedData.Length);

            byte[] key = GenerateKey(password, salt);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (MemoryStream memoryStream = new MemoryStream(encryptedData))
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (StreamReader streamReader = new StreamReader(cryptoStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        private static byte[] GenerateRandomSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        private static byte[] GenerateKey(string password, byte[] salt)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 10000))
            {
                return deriveBytes.GetBytes(32); // 256 bits
            }
        }
    }
}

