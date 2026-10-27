/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

using System.Security.Cryptography;
using System.IO;
using System.Text;

namespace Ubriaco.Util;

public static class ECSUtil
{
    public class EncryptedData
    {
        public int Version { get; set; } = 1;
        public string Signature { get; set; } = string.Empty;
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

    // Generate once on your server. Never distribute this pair's PRIV.
    // Use a separate pair from the ECDH encryption keys.
    public static KP GenerateSigningKeyPair()
    {
        using ECDsa signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return new KP
        {
            PUB = Convert.ToBase64String(signer.ExportSubjectPublicKeyInfo()),
            PRIV = Convert.ToBase64String(signer.ExportPkcs8PrivateKey())
        };
    }

    // SERVER ONLY: encrypt, then sign the complete encrypted envelope.
    public static EncryptedData Encrypt(
        string plainText,
        string receiverPublicKey,
        string signingPrivateKey)
    {
        EncryptedData data = EncryptCore(plainText, receiverPublicKey);
        using ECDsa signer = ECDsa.Create();
        signer.ImportPkcs8PrivateKey(Convert.FromBase64String(signingPrivateKey), out _);
        RequireSigningCurve(signer);
        data.Signature = Convert.ToBase64String(signer.SignData(
            GetSigningBytes(data), HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence));
        return data;
    }

    // CLIENT: trustedSigningPublicKey must come from the application, never the file.
    // There is deliberately no unsigned-decryption overload or fallback.
    public static string Decrypt(
        EncryptedData encryptedData,
        string receiverPrivateKey,
        string trustedSigningPublicKey)
    {
        if (!VerifySignature(encryptedData, trustedSigningPublicKey))
            throw new CryptographicException("Configuration signature is missing or invalid.");

        return DecryptCore(encryptedData, receiverPrivateKey);
    }

    public static bool VerifySignature(EncryptedData data, string trustedSigningPublicKey)
    {
        if (data is null || data.Version != 1 || string.IsNullOrWhiteSpace(data.Signature))
            return false;

        try
        {
            using ECDsa verifier = ECDsa.Create();
            verifier.ImportSubjectPublicKeyInfo(
                Convert.FromBase64String(trustedSigningPublicKey), out _);
            RequireSigningCurve(verifier);
            return verifier.VerifyData(
                GetSigningBytes(data), Convert.FromBase64String(data.Signature),
                HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        }
        catch (CryptographicException) { return false; }
        catch (FormatException) { return false; }
        catch (ArgumentException) { return false; }
    }

    private static void RequireSigningCurve(ECDsa key)
    {
        if (key.ExportParameters(false).Curve.Oid.Value != "1.2.840.10045.3.1.7")
            throw new CryptographicException("Signing requires an ECDSA P-256 key.");
    }

    // Cross-language signature format (NOT JSON):
    // ASCII "RTVCP-EC-CONFIG\0", uint32 LE version, then four fields:
    // public key (SPKI DER), nonce, tag, ciphertext, each Base64-decoded
    // and prefixed with its uint32 LE byte length.
    // ECDSA P-256 / SHA-256 signature uses ASN.1 DER encoding (OpenSSL compatible).
    private static byte[] GetSigningBytes(EncryptedData data)
    {
        if (data.Version != 1)
            throw new CryptographicException("Unsupported configuration version.");

        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(Encoding.ASCII.GetBytes("RTVCP-EC-CONFIG\0"));
        writer.Write((uint)data.Version);
        foreach (string field in new[] { data.PublicKey, data.Nonce, data.Tag, data.CipherText })
        {
            byte[] bytes = Convert.FromBase64String(field);
            writer.Write((uint)bytes.Length);
            writer.Write(bytes);
        }
        writer.Flush();
        return stream.ToArray();
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

    private static EncryptedData EncryptCore(string plainText, string receiverPublicKey)
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

    private static string DecryptCore(
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