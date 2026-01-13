using System.Text.RegularExpressions;
using Ubriaco.Toolkit;

namespace Ubriaco.Toolkit;

[Tool("process-html")]
public class ProcessHTML : ToolBase
{
    public override async Task RunAsync(string[] args)
    {
        if (args.Length < 2)
            throw new Exception("Missing argument (2): views-src path.");

        var files = Directory.GetFiles(args[1], "*.html", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            Console.WriteLine("Processing: " + file);
            var body = System.IO.File.ReadAllText(file);
            try
            {

                var matches = Regex.Matches(body, ">([^<]+)<");

                HTMLUtil.ProcessMatches(ref body, matches);

                System.IO.File.WriteAllText(file, body);
                Console.WriteLine("Success: " + file);
            }
            catch
            {
                Console.WriteLine("Error processing: " + file);
            }
        }
    }
}