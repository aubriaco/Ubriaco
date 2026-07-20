using System.Text.RegularExpressions;
using Ubriaco.Toolkit;
using Ubriaco.Util;

namespace Ubriaco.Toolkit;

[Tool("ec-generate")]
public class ECGenerate : ToolBase
{
    public override async Task RunAsync(string[] args)
    {
        var kp = ECUtil.GenerateKeyPair();
        await File.WriteAllTextAsync("output.txt", $"PUB-ENC: {kp.PUB}\nPRIV-DEC: {kp.PRIV}\n");

        Console.WriteLine("Wrote to file: output.txt");

        var data = ECUtil.Encrypt("test1", kp.PUB);

        Console.WriteLine("Encrypt OK: " + data.CipherText);

        var dec = ECUtil.Decrypt(data, kp.PRIV);

        Console.WriteLine("Decrypt OK: " + dec);
    }
}