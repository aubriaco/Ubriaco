/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
 namespace Ubriaco.Test
{
    public abstract class BaseTest
    {
        private string Name;

        public BaseTest(string name)
        {
            Name = name;
        }

        public abstract Task RunAsync(string[] args);

        public void LogLine(string str)
        {
            Console.WriteLine($"{Name}: {str}");
        }
    }
}