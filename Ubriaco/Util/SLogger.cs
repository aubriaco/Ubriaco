/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

namespace Ubriaco.Util;

public static class SLogger
{
    public static bool Verbose { get; set; } = false;

    public static string? Name { get; set; }

    public static string? LogToFile { get; set; }

    private static FileStream? FS;
    private static StreamWriter? SW;

    public static void Log(string message, bool verbose = true)
    {
        if (verbose && !Verbose) return;
        OpenFileIfNotOpen();
        if (!string.IsNullOrEmpty(Name))
        {
            WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {Name}: {message}");
            return;
        }
        WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
    }

    public static void End()
    {
        CloseFileIfOpen();
    }

    private static void WriteLine(string message)
    {
        if (SW == null)
            Console.WriteLine(message);
        else
            SW.WriteLine(message);
    }

    private static void OpenFileIfNotOpen()
    {
        if (LogToFile == null) return;
        FS = File.Open(LogToFile, FileMode.Append);
        SW = new StreamWriter(FS);
    }

    private static void CloseFileIfOpen()
    {
        if (SW != null) SW.Dispose();
        if (FS != null) FS.Dispose();
    }
}