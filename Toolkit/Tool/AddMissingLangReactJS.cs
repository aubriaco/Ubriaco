using System.Text.RegularExpressions;
using Ubriaco.Toolkit;

namespace Ubriaco.Toolkit;

[Tool("add-missing-lang-reactjs")]
public class AddMissingLangReactJS : ToolBase
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


                if (!body.Contains("import L from"))
                {
                    if (body.Contains("import BaseCom from \"../BaseCom\";"))
                    {
                        int idx = body.IndexOf("import BaseCom from \"../BaseCom\";");
                        body = body.Insert(idx, "import L from \"../Lang\"\n");
                    }
                    else if (body.Contains("import BaseCom from \"./BaseCom\";"))
                    {
                        int idx = body.IndexOf("import BaseCom from \"./BaseCom\";");
                        body = body.Insert(idx, "import L from \"./Lang\"\n");
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