using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Ubriaco.Toolkit;
using Ubriaco.Util;

namespace Ubriaco.Toolkit;

[Tool("ec-encrypt")]
public class ECEncrypt : ToolBase
{
    public override async Task RunAsync(string[] args)
    {
        if (args.Length < 3)
            throw new Exception("Missing argument (3): src dst pubkey");

        var src = args[0];
        var dst = args[1];
        var pubkey = args[2];

        var enc = ECUtil.Encrypt(await File.ReadAllTextAsync(src), pubkey);
        await File.WriteAllTextAsync(dst, JsonConvert.SerializeObject(enc));

        Console.WriteLine("DONE");
    }
}