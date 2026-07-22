/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

using Newtonsoft.Json;

namespace Ubriaco.Toolkit;

public class Config
{
    public static Config I { get; set; } = new Config();

    public class _AI
    {
        public string Host { get; set; } = "http://127.0.0.1:11434";
    }

    public _AI AI { get; set; } = new _AI();

    public static void Load(string fn = "toolkit-config.json")
    {
        var o = JsonConvert.DeserializeObject<Config>(File.ReadAllText(fn));
        if (o == null) throw new Exception("Failed to load config.");
        I = o;
    }
}