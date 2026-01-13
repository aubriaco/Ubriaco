/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
 


namespace Ubriaco.Attributes
{
    public class ModelInfo : Attribute
    {
        public string? Title { get; set; }
        public string? CollectionTitle { get; set; }
        public string? Group { get; set; }
    }
}
