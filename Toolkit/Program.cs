/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
using CsvHelper;
using Newtonsoft.Json;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Ubriaco.Toolkit;
using Ubriaco.Toolkit.Models;

if (args.Length < 1)
    throw new Exception("Missing arguments.");


var assemblies = AppDomain.CurrentDomain.GetAssemblies();

foreach(var i in assemblies)
{
    var types = i.GetTypes().Where(x => x.GetCustomAttribute<Tool>() != null);
    foreach(var type in types)
    {
        Console.WriteLine("TYPE: " + type.Name);
        var att = type.GetCustomAttribute<Tool>();

        if(att != null && args[0] == att.CMD)
        {
            Console.WriteLine("Found action: " + att.CMD);
            var obj = (ToolBase?)Activator.CreateInstance(type);
            if(obj == null)
                throw new Exception("What?");
            await obj.RunAsync(args);
        }
    }
}
