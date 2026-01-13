/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
 namespace Ubriaco.Test.Attributes
{
    public class RunTest : Attribute
    {
        public RunTest(string name)
        {
            Name = name;
        }
        
        public string? Name { get; set; }
    }
}