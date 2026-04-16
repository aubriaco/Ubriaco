using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using Newtonsoft.Json;
using Ubriaco.Toolkit;
using Ubriaco.Toolkit.Models;

namespace Ubriaco.Toolkit;

[Tool("translate-reactjs")]
public class TranslateReactJS : ToolBase
{
    public override async Task RunAsync(string[] args)
    {
        if (args.Length < 2)
            throw new Exception("Missing argument (2): views-src path.");

        var used = new List<string>();
        var dict = new Dictionary<string, LangItem>();

        if (args.Length >= 3)
        {
            var originalDictFile = args[2];
            if (File.Exists(originalDictFile))
            {
                var originalDictData = File.ReadAllText(originalDictFile, Encoding.UTF8);
                dict = JsonConvert.DeserializeObject<Dictionary<string, LangItem>>(originalDictData);
                if (dict == null)
                    throw new Exception("Original dict data is null.");

                foreach (var kvp in dict)
                    used.Add(kvp.Key);
            }
        }

        bool useMD5 = false;
        if (args.Length >= 4)
        {
            var extraArg = args[3];
            if (extraArg == "md5")
            {
                useMD5 = true;
                Console.WriteLine("Using MD5 for dict keys.");
            }
        }

        if (File.Exists("dict.json")) File.Delete("dict.json");


        LangUtil.Init("google-cloud.json");

        var files = Directory.GetFiles(args[1], "*.jsx", SearchOption.AllDirectories);
        using (var jsonFile = File.OpenWrite("dict.json"))
        {
            using (var jsonWriter = new StreamWriter(jsonFile, Encoding.UTF8))
            {
                using (var csvFile = File.OpenWrite("dict.csv"))
                {
                    using (var sw = new StreamWriter(csvFile, Encoding.UTF8))
                    {
                        using (var csv = new CsvWriter(sw, CultureInfo.InvariantCulture))
                        {
                            csv.WriteField("English");
                            csv.WriteField("Spanish");
                            csv.NextRecord();
                            foreach (var file in files)
                            {
                                Console.WriteLine(file);
                                var body = System.IO.File.ReadAllText(file);
                                try
                                {

                                    var regexList = new string[] { "<L>([^<]+)<\\/L>",
                                    "label=\"([^\"]+)\"",
                                    "title=\"([^\"]+)\"",
                                    "placeholder=\"([^\"]+)\"",
                                    "Globals\\.getLang\\('([^\\)]+)'\\)",
                                    "getLang\\('([^\\)]+)'\\)",
                                };

                                    foreach (var regex in regexList)
                                    {

                                        var matches = Regex.Matches(body, regex);


                                        foreach (Match match in matches)
                                        {
                                            string en = match.Groups[1].Value;

                                            if (en.Contains("{"))
                                                continue;

                                            en = en.Replace("\\'", "'");


                                            var dictName = en;
                                            if (useMD5)
                                            {
                                                using (var hash = MD5.Create())
                                                {
                                                    var hashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(en));
                                                    dictName = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                                                }
                                            }

                                            if (used.Contains(dictName))
                                                continue;
                                            used.Add(dictName);
                                            string es = LangUtil.Translate(en, "es-ES");



                                            dict.Add(dictName, new LangItem()
                                            {
                                                en = en,
                                                es = es
                                            });
                                            csv.WriteField(en);
                                            csv.WriteField(es);
                                            csv.NextRecord();
                                            Console.WriteLine("EN: " + en);
                                            Console.WriteLine("ES: " + es);
                                        }
                                    }

                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Error processing: " + file + ": " + ex.Message);
                                }
                            }
                            csvFile.Flush();

                        }
                    }
                }
                jsonWriter.Write(JsonConvert.SerializeObject(dict));
                jsonWriter.Flush();
            }
            Console.WriteLine("DONE");
        }

    }
}