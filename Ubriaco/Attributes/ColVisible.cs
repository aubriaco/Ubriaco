/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
 


namespace Ubriaco.Attributes
{
    public class ColVisible : Attribute
    {
        public string? Title = null;
        public bool Editable = true;
        public string? LinkedModelName = null;
        public Type? LinkedModelType = null;
        public string LinkedModelKey = "ID";
        public string LinkedModelTitle = "Name";
        public string? LinkedModelSort = null;
        public bool LinkedModelUnique = false;
        public bool TableVisible = true;
        public string? Note = null;
        public bool Textarea = false;
        public bool Uploader = false;
        public string? CustomInput = null;
        public bool Required = true;
        public string? Click = null;

        public ColVisible() { }

        public ColVisible(string title, bool editable = true, string? linkedModelName = null, bool tableVisible = true, string note = null, bool textarea = false, bool uploader = false)
        {
            Title = title;
            Editable = editable;
            TableVisible = tableVisible;
            LinkedModelName = linkedModelName;
            Note = note;
            Textarea = textarea;
            Uploader = uploader;
        }
    }
}
