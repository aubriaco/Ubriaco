using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Newtonsoft.Json;
using Ubriaco.Toolkit;

namespace Ubriaco.Toolkit;

[Tool("test-portal")]
public class TestPortal : ToolBase
{
    protected ToolConfig? TConfig;
    protected IBrowser? Browser;
    protected IPage? Page;
    protected Dictionary<string, TestResult> TestResults = new Dictionary<string, TestResult>();
    protected string BaseURL = string.Empty;

    public override async Task RunAsync(string[] args)
    {
        if (args.Length < 1)
            throw new Exception("Missing arguments (1): config-file");

        var configPath = args[1];

        var configData = await File.ReadAllTextAsync(configPath);

        TConfig = JsonConvert.DeserializeObject<ToolConfig>(configData);

        if (TConfig == null) throw new Exception("FAILED TO LOAD CONFIG");

        var uri = new Uri(TConfig.LoginURL);

        BaseURL = $"{uri.Scheme}://{uri.Host}";

        using var playwright = await Playwright.CreateAsync();

        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true
        });


        Browser = browser;

        TestResults.Add(TConfig.LoginURL, new TestResult()
        {
            URL = TConfig.LoginURL,
            IsLogin = true
        });

        await BrowseAndTest();

    }

    protected async Task BrowseAndTest()
    {
        while (TestResults.Any(x => !x.Value.IsTested))
        {
            var tr = TestResults.FirstOrDefault(x => !x.Value.IsTested);
            try
            {
                await BrowseAndTestProcess(tr.Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine("EXCEPTION: " + ex.Message);
            }
            await Task.Delay(1000);
        }
    }

    protected async Task BrowseAndTestProcess(TestResult tr)
    {
        if (TConfig == null) throw new Exception("WHAT CONFIG?");
        if (Browser == null) throw new Exception("WHAT BROWSER?");

        if (Page == null)
            Page = await Browser.NewPageAsync();

        var page = Page;



        Console.WriteLine("Browse: " + tr.URL);
        await page.GotoAsync(tr.URL);

        if (tr.IsLogin)
        {
            Console.WriteLine("Is Login");
            await page.FillAsync(TConfig.UsernameSelector, TConfig.Username);
            await page.FillAsync(TConfig.PasswordSelector, TConfig.Password);
            await page.ClickAsync(TConfig.SubmitSelector);
        }

        var content = await page.ContentAsync();

        //Console.WriteLine("CONTENT: " + content);

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);


        content = await page.ContentAsync();
        Console.WriteLine("PAGE URL: " + page.Url);

        //Console.WriteLine("CONTENT (2): " + content);

        page.PageError += (_, e) =>
        {
            Console.WriteLine("JS ERROR: " + e);
            tr.PageError += e;
        };

        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                Console.WriteLine("CONSOLE ERROR: " + msg.Text);
                tr.ConsoleError += msg.Text;
            }
        };

        // set is tested
        tr.IsTested = true;

        var bodyOpenIdx = content.ToLower().IndexOf("<body>");
        var bodyCloseIdx = content.ToLower().IndexOf("</body>");

        var regex = new Regex(@"href=""([^""]+)""");

        var matches = regex.Matches(content);

        foreach (Match match in matches)
        {
            if (match.Index > bodyOpenIdx && match.Index < bodyCloseIdx)
            {
                var url = match.Groups[1].Value;
                if (!TestResults.ContainsKey(BaseURL + url))
                {
                    Console.WriteLine("Adding URL: " + BaseURL + url);
                    TestResults.Add(BaseURL + url, new TestResult()
                    {
                        URL = BaseURL + url
                    });
                }
            }
        }
    }

    // Classes

    protected class ToolConfig
    {
        public string LoginURL { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string UsernameSelector { get; set; } = "input[name='username']";
        public string PasswordSelector { get; set; } = "input[password='username']";
        public string SubmitSelector { get; set; } = "button[type='submit']";
    }

    protected class TestResult
    {
        public string URL { get; set; } = string.Empty;
        public bool IsTested { get; set; } = false;
        public bool IsError { get; set; } = false;
        public bool IsLogin { get; set; } = false;
        public string PageError { get; set; } = string.Empty;
        public string ConsoleError { get; set; } = string.Empty;
    }
}