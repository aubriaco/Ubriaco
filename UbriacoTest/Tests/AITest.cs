/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

using Newtonsoft.Json;
using Ubriaco.Test;
using Ubriaco.Test.Attributes;
using Ubriaco.Util;

[RunTest("ai")]
public class AITest : BaseTest
{
    public AITest(string name) : base(name)
    {
    }

    public override async Task RunAsync(string[] args)
    {

        var ai = new AIU("http://192.168.0.7:11434");

        var resp = await ai.SendRequestReturnDetail(@"Translate the following message from English to Spanish, return only the result:
Hello how are you?", "gemma4");


        Console.WriteLine(JsonConvert.SerializeObject(resp));

    }
}