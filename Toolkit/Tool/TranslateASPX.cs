
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CsvHelper;
using Newtonsoft.Json;
using Ubriaco.Toolkit.Models;


namespace Ubriaco.Toolkit;

[Tool("translate-aspx")]
public class TranslateASPX : ToolBase
{
    private Dictionary<string, LangItem> Dict = new Dictionary<string, LangItem>();

    public override async Task RunAsync(string[] args)
    {
        if (args.Length < 2)
            throw new Exception("Missing argument (2): src path.");

        var used = new List<string>();

        if (args.Length >= 3)
        {
            var originalDictFile = args[2];
            var originalDictData = File.ReadAllText(originalDictFile, Encoding.UTF8);
            var dict = JsonConvert.DeserializeObject<Dictionary<string, LangItem>>(originalDictData);
            if (dict == null)
                throw new Exception("Original dict data is null.");

            Dict = dict;

            foreach (var kvp in Dict)
                used.Add(kvp.Key);
        }

        if (File.Exists("dict.json")) File.Delete("dict.json");

        LangUtil.Init("google-cloud.json");

        var files = Directory.GetFiles(args[1], "*.aspx", SearchOption.AllDirectories);
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
                                Console.WriteLine("FILE: " + file);   
                                ProcessFile(file);
                                

                            }
                            csvFile.Flush();

                        }
                    }
                }
                jsonWriter.Write(JsonConvert.SerializeObject(Dict));
                jsonWriter.Flush();
            }
            Console.WriteLine("DONE");
        }
        
    }

    private class TRA
    {
        public string Content;
        public string Phrase
        {
            get
            {
                return Content.Trim();
            }
        }
        public int Index;

        public int Length
        {
            get
            {
                return Content.Length;
            }
        }
    }
    

    private void ProcessFile(string file)
    {
        if(file.EndsWith("frmPatientTM.aspx") || file.EndsWith("frmPatientTMV2.aspx"))
            return;

        if(file.Contains("\\obj\\") || file.Contains("/obj/") || file.Contains("\\bin\\") || file.Contains("/bin/"))
            return;
        var body = System.IO.File.ReadAllText(file);
        var regex = new Regex(@">([^<]+)");

        Console.WriteLine("Cropping scripts and styles...");
        var scriptsAndStyles = CropScriptsAndStyles(ref body);
        var list = new List<TRA>();

        Console.WriteLine("Matching...");
        var matches = regex.Matches(body);

        foreach(Match match in matches)
        {
            var val = match.Groups[1].Value;
            if(string.IsNullOrWhiteSpace(val))
                continue;
            if(val.Contains("{") || val.Contains("}") || val.Contains("<") || val.Contains(">") || val.Contains(";") || val.Contains('\'') || val.Contains("#") || val.Contains("&&")
                || val.Contains("+") || val.Contains("=") || val.ToUpper() == "ID")
                continue;
            if(!ShouldTranslate(body, match.Groups[1].Index))
                continue;
            Console.WriteLine("MATCH: " + match.Groups[1].Value);
            try
            {
                if(IsTranslated(body, match.Groups[1].Value))
                {
                    Console.WriteLine("Already translated.");
                    continue;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("ERR: " + ex.Message);
                continue;
            }


            list.Add(new TRA()
            {
                Content = match.Groups[1].Value,
                Index = match.Groups[1].Index
            });
        }

        list.Sort((a,b) => b.Content.Length.CompareTo(a.Content.Length));

        foreach(var phrase in list)
        {
            ProcessPhrase(phrase.Phrase);
            body = body.Remove(phrase.Index, phrase.Length).Insert(phrase.Index, @"<span class=""u-lang"">" + phrase.Content + "</span>");
            ShiftList(list, phrase.Index, 28);
            ShiftList(scriptsAndStyles, phrase.Index, 28);
        }

        PushScriptsAndStyles(ref body, scriptsAndStyles);

        File.WriteAllText(file, body);
    }

    private List<TRA> CropScriptsAndStyles(ref string body)
    {
        var list = new List<TRA>();
        

        // Scripts
        Console.WriteLine("Scripts...");
        var regex = new Regex(@"<script((.|\n|\r)*?)>((.|\n|\r)*?)<\/script>", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        var matches = regex.Matches(body);
        Console.WriteLine("Match count: " + matches.Count);
        foreach(Match match in matches)
        {
            Console.WriteLine("Script match: " + match.Index);
            list.Add(new TRA()
            {
                Content = match.Value,
                Index = match.Index
            });
        }

        // Styles
        /*
        Console.WriteLine("Styles...");
        regex = new Regex(@"<style((.|\n|\r)*?)>((.|\n|\r)*?)<\/style>");
        matches = regex.Matches(body);
        foreach(Match match in matches)
        {
            list.Add(new TRA()
            {
                Content = match.Value,
                Index = match.Index
            });
        }
        */

        list.Reverse();

        foreach(var i in list)
        {
            Console.WriteLine("Shift: " + i.Length);
            body = body.Remove(i.Index, i.Length);
            ShiftList(list, i.Index, (-1) * i.Length);
        }


        return list;
    }

    private void PushScriptsAndStyles(ref string body, List<TRA> list)
    {
        foreach(var i in list)
        {
            body = body.Insert(i.Index, i.Content);
        }
    }

    private void ShiftList(List<TRA> list, int cur, int shift)
    {
        foreach(var i in list)
        {
            if(i.Index > cur)
            {
                i.Index += shift;
            }
        }
    }

    public void ProcessPhrase(string str)
    {
        if(Dict.ContainsKey(str))
            return;

        Console.WriteLine("Translating: " + str);
        string tra = LangUtil.Translate(str.Trim(), "es-ES");
        Dict.Add(str, new LangItem() {
            en = str,
            es = tra
        });
    }

    public bool ShouldTranslate(string body, int n)
    {
        return true;
    }

    public bool IsTranslated(string body, string val)
    {
        var n = body.IndexOf(val);
        return body.Substring(n-21, 21) == "<span class=\"u-lang\">";
    }

}