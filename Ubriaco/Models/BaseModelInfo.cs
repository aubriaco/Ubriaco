/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
 

namespace Ubriaco.Models
{
    public class BaseModelInfo
    {
        public string? Title { get; set; }
        public string? CollectionTitle { get; set; }
        public string? Name { get; set; }
        public string? Ident
        {
            get
            {
                if (Name == null)
                    return null;
                else
                    return Name.ToLower();
            }
        }
        public string? Group { get; set; } = "Unsorted";
        public string? Description { get; set; }
        public List<ModelVarInfo>? Variables { get; set; }
    }
}
