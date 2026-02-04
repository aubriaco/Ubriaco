/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

using System.Diagnostics;
using System.Text;

namespace Ubriaco.Util;

public static class CertUtil
{
    public static async Task<byte[]> GenerateAsync()
    {
        var uuid = Guid.NewGuid().ToString();
        var outputDir = ".";
        Directory.CreateDirectory(outputDir);

        var keyPath = Path.Combine(outputDir, uuid + ".cert.key");
        var crtPath = Path.Combine(outputDir, uuid + ".cert.crt");
        var pemPath = Path.Combine(outputDir, uuid + ".cert.pem");
        var pfxPath = Path.Combine(outputDir, uuid + ".client.pfx");

        await RunProcess(
            fileName: "openssl",
            arguments:
                "req -new -newkey rsa:4096 -days 36500 -nodes -x509 " +
                "-passout pass:0 " +
                $"-keyout \"{keyPath}\" -out \"{crtPath}\" " +
                "-subj \"/C=US/ST=Puerto Rico/L=San Juan/O=Solusek Systems/OU=IT/CN=solusek.com\"",
            extraEnv: ("MSYS_NO_PATHCONV", "1")
        );

        await CombineFiles(pemPath, keyPath, crtPath);

        await RunProcess(
            fileName: "openssl",
            arguments:
                "pkcs12 -export " +
                $"-in \"{pemPath}\" -out \"{pfxPath}\" " +
                "-name \"Solusek\" " +
                "-passin pass:0 -passout pass:0"
        );

        var data = await File.ReadAllBytesAsync(pfxPath);

        File.Delete(keyPath);
        File.Delete(crtPath);
        File.Delete(pemPath);
        File.Delete(pfxPath);

        return data;
    }

    private static async Task CombineFiles(string outputFile, params string[] inputs)
    {
        using var outStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None);
        foreach (var input in inputs)
        {
            using var inStream = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.Read);
            await inStream.CopyToAsync(outStream);
        }
    }

    private static async Task RunProcess(string fileName, string arguments, params (string Key, string Value)[] extraEnv)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var (k, v) in extraEnv)
            psi.Environment[k] = v;

        using var p = new Process { StartInfo = psi };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        p.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        p.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        if (!p.Start())
            throw new InvalidOperationException($"Failed to start process: {fileName}");

        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        await p.WaitForExitAsync();

        if (p.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Command failed (exit {p.ExitCode}): {fileName} {arguments}\n" +
                $"STDOUT:\n{stdout}\nSTDERR:\n{stderr}"
            );
        }
    }
}
