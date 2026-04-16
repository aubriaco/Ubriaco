using System.Text.RegularExpressions;
using Ubriaco.Toolkit;

namespace Ubriaco.Toolkit;

[Tool("process-reactjs")]
public class ProcessReactJS : ToolBase
{
    public override async Task RunAsync(string[] args)
    {
        if (args.Length < 2)
            throw new Exception("Missing argument (2): react-src/src path.");

        var files = Directory.GetFiles(args[1], "*.jsx", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            Console.WriteLine("Processing: " + file);
            var body = System.IO.File.ReadAllText(file);
            try
            {

                var matches = Regex.Matches(body, ">([^<]+)<");

                HTMLUtil.ProcessMatchesReactJS(ref body, matches);

                body = body.Replace("<L><L>", "<L>").Replace("</L></L>", "</L>");


                //if (!body.Contains("import L from"))
                //{
                //int idx = body.IndexOf("*--------------------------------------------------------------------------------------------*/\n");
                //body = body.Insert(idx, "\nimport L from \"./Lang\"\n");
                //}

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