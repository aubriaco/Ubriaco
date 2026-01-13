/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
 

using Newtonsoft.Json;

namespace Ubriaco.Util
{
    public static class VersionUtil
    {
        private static long _Version = 0;

        public static long Version
        {
            get
            {
                if(_Version == 0)
                {
                    try
                    {
                        if(File.Exists("version.json"))
                        {
                            var data = File.ReadAllText("version.json");
                            dynamic? json = JsonConvert.DeserializeObject(data);
                            if(json != null)
                            {
                                if(json.version != null)
                                    _Version = (long)json.version;
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("Failed to read version: " + ex.Message);
                    }
                }
                return _Version;
            }
        }
    }
}