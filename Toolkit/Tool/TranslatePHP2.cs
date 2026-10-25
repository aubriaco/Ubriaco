
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using Newtonsoft.Json;
using Ubriaco.Toolkit.Models;


namespace Ubriaco.Toolkit;

[Tool("translate-php-2")]
public class TranslatePHP2 : ToolBase
{
    public override async Task RunAsync(string[] args)
    {
        if (args.Length < 2)
            throw new Exception("Missing arguments (3): translate-php-2 scan-path orig-dict-json-file");

        var used = new List<string>();
        var dict = new Dictionary<string, LangItem>();

        if (args.Length >= 3)
        {
            var originalDictFile = args[2];
            var originalDictData = File.ReadAllText(originalDictFile, Encoding.UTF8);
            dict = JsonConvert.DeserializeObject<Dictionary<string, LangItem>>(originalDictData);
            if (dict == null)
                throw new Exception("Original dict data is null.");

            foreach (var kvp in dict)
                used.Add(kvp.Key);
        }

        if (File.Exists("dict.json")) File.Delete("dict.json");

        //LangUtil.Init("google-cloud.json");

        var files = Directory.GetFiles(args[1], "*.php", SearchOption.AllDirectories);
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
                                    var matches = Regex.Matches(body, ">([^<]+)<([^>]+)");

                                    Console.WriteLine("MATCH COUNT: " + matches.Count);

                                    foreach (Match match in matches)
                                    {
                                        string en = match.Groups[1].Value;
                                        string post = match.Groups[2].Value;
                                        if (string.IsNullOrWhiteSpace(en)
                                            || en.Contains('>')
                                            || en.Replace(" ", "").Contains("function()")
                                            || en.Contains("{")
                                            || en.Contains("}")
                                            || post.ToLower().Contains("title")
                                            || post.ToLower().Contains("script")
                                             || post.ToLower().Contains("style"))
                                            continue;


                                        Console.WriteLine("EN: " + en);
                                        en = en.Replace("\\'", "'").Replace("\n", " ").Replace("\r", " ").Trim();
                                        while (en.Contains("  ")) en = en.Replace("  ", " ");
                                        if (string.IsNullOrWhiteSpace(en)) continue;
                                        if (used.Contains(en))
                                            continue;
                                        used.Add(en);
                                        string es = await LangUtil.TranslateAI(en, "Spanish");
                                        dict.Add(en, new LangItem()
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