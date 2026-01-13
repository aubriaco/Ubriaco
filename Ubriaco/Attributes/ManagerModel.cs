/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
 


namespace Ubriaco.Attributes
{
    public class ManagerModel : Attribute
    {
        public string EditUrl;

        public ManagerModel(string editUrl)
        {
            EditUrl = editUrl;
        }
    }
}
