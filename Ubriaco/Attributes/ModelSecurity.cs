/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
 


namespace Ubriaco.Attributes
{
    public class ModelSecurity : Attribute
    {
        public string Section, AdminSection;

        public ModelSecurity(string section, string adminSection = null)
        {
            Section = section;
            if (adminSection == null)
                AdminSection = section;
            else
                AdminSection = adminSection;
        }
    }
}
