/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
 


namespace Ubriaco.Attributes
{
    public class FormObject : Attribute
    {
        public string? Title { get; set; }
        public string? SubmitUrl { get; set; }
    }
}
