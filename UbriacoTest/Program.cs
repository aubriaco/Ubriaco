/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

using System.Reflection;
using Ubriaco.Test;
using Ubriaco.Test.Attributes;

if (args.Length < 1)
{
    Console.WriteLine("No test specified.");
    return;
}

var assemblies = AppDomain.CurrentDomain.GetAssemblies();

bool ran = false;

foreach (var assembly in assemblies)
{
    var type = assembly.GetTypes().FirstOrDefault(t => t.GetCustomAttribute<RunTest>()?.Name == args[0]);

    if (type == null)
        continue;

    var test = (BaseTest?)Activator.CreateInstance(type, new object[] { args[0] });

    if (test == null)
        throw new Exception("Test creation failed.");

    try
    {
        await test.RunAsync(args);
        ran = true;
    }
    catch(Exception ex)
    {
        Console.WriteLine("TEST ERROR: " + ex.Message);
        Console.WriteLine("TEST STACK: " + ex.StackTrace);
    }
}

if(!ran)
    Console.WriteLine("Found no test to run: " + args[0]);
    