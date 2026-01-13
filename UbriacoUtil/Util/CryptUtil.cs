/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
 

using System.Security.Cryptography;
using System.Text;

namespace Ubriaco.Util
{
    public class CryptUtil
    {
        private static string Salt { get; set; } = "6238fcf6-72c9-4d97-a39b-f6ed9c45db93";

        public static string HashPassword(string s)
        {
            using (var sham = SHA256.Create())
            {
                return Convert.ToBase64String(sham.ComputeHash(Encoding.UTF8.GetBytes(Salt + s)));
            }
        }

        private static byte[] IV = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public static string Encrypt(string plainText, string secret)
        {
            var key = Encoding.ASCII.GetBytes(secret);
            if (key.Length < 32)
            {
                var _key = new List<byte>();
                _key.AddRange(key);
                for (var n = key.Length; n < 32; n++)
                {
                    _key.Add(0);
                }
                key = _key.ToArray();
            }

            return Convert.ToBase64String(EncryptStringToBytes_Aes(plainText, key, IV), Base64FormattingOptions.None);
        }

        public static string Decrypt(string encrypted, string secret)
        {
            var key = Encoding.ASCII.GetBytes(secret);
            if (key.Length < 32)
            {
                var _key = new List<byte>();
                _key.AddRange(key);
                for (var n = key.Length; n < 32; n++)
                {
                    _key.Add(0);
                }
                key = _key.ToArray();
            }

            return DecryptStringFromBytes_Aes(Convert.FromBase64String(encrypted), key, IV);
        }

        static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create an encryptor to perform the stream transform.
                var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }

        static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decryptor to perform the stream transform.
                var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }
    }
}
