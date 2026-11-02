/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Ubriaco.Util
{
    public class CryptUtil
    {
        private const string LegacySalt = "6238fcf6-72c9-4d97-a39b-f6ed9c45db93";
        private const string PasswordPrefix = "UBPBKDF2$1$";
        private const int PasswordIterations = 600000;
        private const string EncryptionPrefix = "UBAESGCM$1$";
        private const int EncryptionIterations = 210000;
        private static readonly byte[] EncryptionHeader = Encoding.ASCII.GetBytes(EncryptionPrefix);

        /// <summary>Creates a salted PBKDF2-SHA256 hash. Use VerifyPassword for comparisons.</summary>
        public static string HashPasswordV2(string s)
        {
            ArgumentNullException.ThrowIfNull(s);
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(s, salt, PasswordIterations, HashAlgorithmName.SHA256, 32);
            return PasswordPrefix + PasswordIterations.ToString(CultureInfo.InvariantCulture) + "$"
                + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
        }

        /// <summary>Verifies both current hashes and the original fixed-salt SHA256 hashes.</summary>
        public static bool VerifyPassword(string password, string storedHash)
        {
            ArgumentNullException.ThrowIfNull(password);
            ArgumentNullException.ThrowIfNull(storedHash);
            try
            {
                if (storedHash.StartsWith(PasswordPrefix, StringComparison.Ordinal))
                {
                    string[] parts = storedHash.Split('$');
                    if (parts.Length != 5 || !int.TryParse(parts[2], NumberStyles.None,
                        CultureInfo.InvariantCulture, out int iterations) || iterations < 1 || iterations > 2000000)
                        return false;
                    byte[] salt = Convert.FromBase64String(parts[3]);
                    byte[] expected = Convert.FromBase64String(parts[4]);
                    if (salt.Length != 16 || expected.Length != 32) return false;
                    byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);
                    return CryptographicOperations.FixedTimeEquals(actual, expected);
                }
                // A legacy hash is exactly one Base64 encoded SHA256 digest.
                byte[] legacyExpected = Convert.FromBase64String(storedHash);
                if (legacyExpected.Length != 32) return false;
                byte[] legacyActual = SHA256.HashData(Encoding.UTF8.GetBytes(LegacySalt + password));
                return CryptographicOperations.FixedTimeEquals(legacyActual, legacyExpected);
            }
            catch (FormatException) { return false; }
        }

        /// <summary>True when a successfully verified password should be hashed again and saved.</summary>
        public static bool PasswordNeedsRehash(string storedHash)
        {
            ArgumentNullException.ThrowIfNull(storedHash);
            return !storedHash.StartsWith(PasswordPrefix + PasswordIterations.ToString(CultureInfo.InvariantCulture) + "$", StringComparison.Ordinal);
        }

        /// <summary>Encrypts with PBKDF2-SHA256 and AES-GCM using a fresh salt and nonce.</summary>
        public static string EncryptV2(string plainText, string secret)
        {
            ArgumentNullException.ThrowIfNull(plainText);
            ArgumentException.ThrowIfNullOrEmpty(secret);
            // Payload layout: salt(16), nonce(12), authentication tag(16), ciphertext.
            byte[] payload = new byte[44 + Encoding.UTF8.GetByteCount(plainText)];
            RandomNumberGenerator.Fill(payload.AsSpan(0, 28));
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(secret, payload.AsSpan(0, 16),
                EncryptionIterations, HashAlgorithmName.SHA256, 32);
            try
            {
                using var aes = new AesGcm(key, 16);
                aes.Encrypt(payload.AsSpan(16, 12), Encoding.UTF8.GetBytes(plainText),
                    payload.AsSpan(44), payload.AsSpan(28, 16), EncryptionHeader);
                return EncryptionPrefix + Convert.ToBase64String(payload);
            }
            finally { CryptographicOperations.ZeroMemory(key); }
        }

        /// <summary>Decrypts current authenticated payloads and original AES-CBC payloads.</summary>
        public static string DecryptV2(string encrypted, string secret)
        {
            ArgumentNullException.ThrowIfNull(encrypted);
            ArgumentException.ThrowIfNullOrEmpty(secret);
            if (!encrypted.StartsWith(EncryptionPrefix, StringComparison.Ordinal))
                return Decrypt(encrypted, secret);

            byte[] payload = Convert.FromBase64String(encrypted.Substring(EncryptionPrefix.Length));
            if (payload.Length < 44) throw new CryptographicException("Invalid encrypted payload.");
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(secret, payload.AsSpan(0, 16),
                EncryptionIterations, HashAlgorithmName.SHA256, 32);
            byte[] plainBytes = new byte[payload.Length - 44];
            try
            {
                using var aes = new AesGcm(key, 16);
                aes.Decrypt(payload.AsSpan(16, 12), payload.AsSpan(44), payload.AsSpan(28, 16),
                    plainBytes, EncryptionHeader);
                return Encoding.UTF8.GetString(plainBytes);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
                CryptographicOperations.ZeroMemory(plainBytes);
            }
        }

        // Legacy API: output and behavior retained for existing integrations.
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
