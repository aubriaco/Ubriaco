/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

using Ubriaco.Test;
using Ubriaco.Test.Attributes;
using Ubriaco.Util;

[RunTest("cert")]
public class CertTest : BaseTest
{
    public CertTest(string name) : base(name)
    {
    }

    public override async Task RunAsync(string[] args)
    {
        var data = await CertUtil.GenerateAsync();
        Console.WriteLine("DATA SIZE: " + data.Length);
    }
}