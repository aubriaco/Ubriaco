/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/



using Ubriaco.Attributes;
using Ubriaco.Data;
using Ubriaco.Models;

namespace Ubriaco.Toolkit
{
    public static class DataManager
    {
        public static void SyncAllModels(BaseConnection c)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes().Where(t => t.IsDefined(typeof(Table), true));
                foreach (var t in types)
                {
                    var model = (BaseModel?)Activator.CreateInstance(t);
                    if (model != null)
                    {
                        model.SyncStructure(c);
                    }
                }
            }
        }

        public static async Task SyncAllModelsAsync(BaseConnection c)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes().Where(t => t.IsDefined(typeof(Table), true));
                foreach (var t in types)
                {
                    var model = (BaseModel?)Activator.CreateInstance(t);
                    if (model != null)
                    {
                        await model.SyncStructureAsync(c);
                    }
                }
            }
        }
    }
}
