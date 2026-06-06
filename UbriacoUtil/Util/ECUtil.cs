/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

using System.Security.Cryptography;

namespace Ubriaco.Util;

public static class ECUtil
{
    public class EncryptedData
    {
        public string PublicKey { get; set; } = string.Empty;
        public string Nonce { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string CipherText { get; set; } = string.Empty;
    }

    public class KP
    {
        public string PUB { get; set; } = string.Empty; // encrypt
        public string PRIV { get; set; } = string.Empty; // decrypt
    }

    public static KP GenerateKeyPair()
    {
        using ECDiffieHellman ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

        return new KP()
        {
            PUB = Convert.ToBase64String(ecdh.ExportSubjectPublicKeyInfo()),
            PRIV = Convert.ToBase64String(ecdh.ExportPkcs8PrivateKey())
        };
    }

    public static EncryptedData Encrypt(string plainText, string receiverPublicKey)
    {
        using ECDiffieHellman sender = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

        using ECDiffieHellman receiver = ECDiffieHellman.Create();
        receiver.ImportSubjectPublicKeyInfo(
            Convert.FromBase64String(receiverPublicKey),
            out _);

        byte[] aesKey = sender.DeriveKeyFromHash(
            receiver.PublicKey,
            HashAlgorithmName.SHA256);

        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] tag = new byte[16];

        byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        byte[] cipherBytes = new byte[plainBytes.Length];

        using AesGcm aes = new(aesKey, 16);

        aes.Encrypt(
            nonce,
            plainBytes,
            cipherBytes,
            tag);

        return new EncryptedData
        {
            PublicKey = Convert.ToBase64String(sender.ExportSubjectPublicKeyInfo()),
            Nonce = Convert.ToBase64String(nonce),
            Tag = Convert.ToBase64String(tag),
            CipherText = Convert.ToBase64String(cipherBytes)
        };
    }

    public static string Decrypt(
        EncryptedData encryptedData,
        string receiverPrivateKey)
    {
        using ECDiffieHellman receiver = ECDiffieHellman.Create();

        receiver.ImportPkcs8PrivateKey(
            Convert.FromBase64String(receiverPrivateKey),
            out _);

        using ECDiffieHellman sender = ECDiffieHellman.Create();

        sender.ImportSubjectPublicKeyInfo(
            Convert.FromBase64String(encryptedData.PublicKey),
            out _);

        byte[] aesKey = receiver.DeriveKeyFromHash(
            sender.PublicKey,
            HashAlgorithmName.SHA256);

        byte[] nonce = Convert.FromBase64String(encryptedData.Nonce);
        byte[] tag = Convert.FromBase64String(encryptedData.Tag);
        byte[] cipherBytes = Convert.FromBase64String(encryptedData.CipherText);

        byte[] plainBytes = new byte[cipherBytes.Length];

        using AesGcm aes = new(aesKey, 16);

        aes.Decrypt(
            nonce,
            cipherBytes,
            tag,
            plainBytes);

        return System.Text.Encoding.UTF8.GetString(plainBytes);
    }
}