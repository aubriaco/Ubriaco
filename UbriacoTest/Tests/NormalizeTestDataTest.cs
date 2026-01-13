
using Ubriaco.Test;
using Ubriaco.Test.Attributes;
using Ubriaco.Util;

[RunTest("normalize-test-data")]
public class NormalizeTestDataTest : BaseTest
{
    public NormalizeTestDataTest(string name) : base(name)
    {
    }

    public override async Task RunAsync(string[] args)
    {
        var dir = Directory.GetFiles("test-data");

        foreach(var fn in dir)
        {
            string nn = fn.Replace(" ","_").ToLower();
            if(fn != nn)
            {
                File.Move(fn, nn);
                Console.WriteLine("Renamed: " + nn);
            }
        }

    }
}