/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
 

using System.Text.Json;

namespace Ubriaco.Models
{
    public class ModelVarInfo
    {
        public string? Name { get; set; }
        public string? Ident
        {
            get
            {
                if (Name == null)
                    return null;
                else
                    return JsonNamingPolicy.CamelCase.ConvertName(Name);
            }
        }
        public string? DataName { get; set; }
        public string? Title { get; set; }
        public string? VarType { get; set; }
        public string? InputType { get; set; }
        public string? Note { get; set; }
        public string? Click { get; set; }
        public string? LinkedModelName { get; set; }
        public string? LinkedModelKey { get; set; }
        public string? LinkedModelTitle { get; set; }
        public string? LinkedModelSort { get; set; }
        public bool LinkedModelUnique { get; set; } = false;
        public bool Required { get; set; } = false;
        public bool Listable { get; set; } = false;
        public bool Readable { get; set; } = false;
        public bool Writable { get; set; } = false;
    }
}
