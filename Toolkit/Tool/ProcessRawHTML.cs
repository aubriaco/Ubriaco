using System.Text.RegularExpressions;

namespace Ubriaco.Toolkit;

[Tool("process-raw-html")]
public class ProcessRawHTML : ToolBase
{
    public override async Task RunAsync(string[] args)
    {
        if (args.Length < 2)
            throw new Exception("Missing argument (2): html dir path.");

        var files = Directory.GetFiles(args[1], "*.html", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            Console.WriteLine("Processing: " + file);
            var body = System.IO.File.ReadAllText(file);
            try
            {

                var matches = Regex.Matches(body, ">([^<]+)<([^>]+)");

                HTMLUtil.ProcessMatchesRawHTML(ref body, matches);

                matches = Regex.Matches(body, @"<span class=""lang""><span class=""lang"">([^<]+)<\/span><\/span>");

                foreach (Match m in matches)
                {
                    while (body.Contains(@"<span class=""lang""><span class=""lang"">" + m.Groups[1].Value + "</span></span>"))
                    {
                        body = body.Replace(@"<span class=""lang""><span class=""lang"">" + m.Groups[1].Value + "</span></span>",
                            @"<span class=""lang"">" + m.Groups[1].Value + "</span>");
                    }
                }

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