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
    protected Queue<TestResult> TestQueue { get; set; } = new Queue<TestResult>();
    protected Dictionary<string, TestResult> TestsResults = new Dictionary<string, TestResult>();

    public override async Task RunAsync(string[] args)
    {
        if (args.Length < 1)
            throw new Exception("Missing arguments (1): config-file");

        var configPath = args[1];

        var configData = await File.ReadAllTextAsync(configPath);

        TConfig = JsonConvert.DeserializeObject<ToolConfig>(configData);



        using var playwright = await Playwright.CreateAsync();

        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true
        });

        Browser = browser;

        TestQueue.Enqueue(new TestResult()
        {
            URL = TConfig.LoginURL,
            IsLogin = true
        });

        await BrowseAndTest();

    }

    protected async Task BrowseAndTest()
    {
        while (TestQueue.Count > 0)
        {
            var tr = TestQueue.Dequeue();
            await BrowseAndTestProcess(tr);
            await Task.Delay(1000);
        }
    }

    protected async Task BrowseAndTestProcess(TestResult tr)
    {
        if (TConfig == null) throw new Exception("WHAT CONFIG?");
        if (Browser == null) throw new Exception("WHAT BROWSER?");
        var page = await Browser.NewPageAsync();
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

        Console.WriteLine("CONTENT: " + content);
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
    }
}