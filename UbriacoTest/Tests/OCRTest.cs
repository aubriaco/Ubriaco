/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

using Ubriaco.Test;
using Ubriaco.Test.Attributes;
using Ubriaco.Util;

[RunTest("ocr")]
public class OCRTest : BaseTest
{
    public OCRTest(string name) : base(name)
    {
    }

    public override async Task RunAsync(string[] args)
    {
        if (args.Length < 2)
            throw new Exception("No argument.");
        var ocr = new OCRUtil("tessdata_fast");
        ocr.Process("spa", await File.ReadAllBytesAsync(args[1]));


    }
}