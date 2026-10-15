using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ChromExplorer.Modules.Auth.Services
{
    public class CryptoHelper
    {
        private static readonly string EncryptionKey = "Chrome@1234".PadRight(32, '0').Substring(0, 32);

        public static string Encrypt(string plainText)
        {
            byte[] iv = new byte[16];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                MemoryStream memoryStream = new MemoryStream();
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                {
                    streamWriter.Write(plainText);
                }

                array = memoryStream.ToArray();
                memoryStream.Dispose();
            }

            return Convert.ToBase64String(array)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        internal static string EncryptWithKey(string plainText, string rawKey)
        {
            string paddedKey = rawKey.PadRight(32, '0').Substring(0, 32);
            byte[] iv = new byte[16];
            byte[] array;
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(paddedKey);
                aes.IV = iv;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                        sw.Write(plainText);
                    array = ms.ToArray();
                }
            }
            return Convert.ToBase64String(array).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        internal static string DecryptWithKey(string cipherText, string rawKey)
        {
            string paddedKey = rawKey.PadRight(32, '0').Substring(0, 32);
            cipherText = cipherText.Replace("-", "+").Replace("_", "/");
            switch (cipherText.Length % 4)
            {
                case 2: cipherText += "=="; break;
                case 3: cipherText += "="; break;
            }
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(cipherText);
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(paddedKey);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using (var ms = new MemoryStream(buffer))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                    return sr.ReadToEnd();
            }
        }

        public static string Decrypt(string cipherText)
        {

            cipherText = cipherText.Replace("-", "+").Replace("_", "/");

            switch (cipherText.Length % 4)
            {
                case 2: cipherText += "=="; break;
                case 3: cipherText += "="; break;
            }

            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
                aes.IV = iv;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                using (StreamReader streamReader = new StreamReader(cryptoStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

    }
}

