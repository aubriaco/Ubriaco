/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
 


namespace Ubriaco.Attributes
{
    public class FormItem : Attribute
    {
        public string? Title;
        public string? Note;
        public string Type = "text";
        public Type ModelType = null;
        public string Placeholder = null;
        public string? Header;
        public List<string>? Options;
        public string Col = "col-12";
        public string? Style = null;
        public bool Required = false;
        public string UploadUrl = "";
        public string DownloadUrl = "";
        public string TestUrl = "";
    }
}
