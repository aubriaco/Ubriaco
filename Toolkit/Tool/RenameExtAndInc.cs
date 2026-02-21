using System.Text.RegularExpressions;
using Ubriaco.Toolkit;

namespace Ubriaco.Toolkit;

[Tool("rename-ext-and-inc")]
public class RenameExt : ToolBase
{
    public override async Task RunAsync(string[] args)
    {
        if (args.Length < 3)
            throw new Exception("Missing arguments (3): path ext1 ext2");

        var fromExt = args[2];
        var toExt = args[3];

        var files = Directory.GetFiles(args[1], "*." + args[2], SearchOption.AllDirectories);
        foreach (var file in files)
        {
            Console.WriteLine("Processing: " + file);
            var to = file.Substring(0, file.Length - args[2].Length) + args[3];
            File.Move(file, to);
        }
    }
}