/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
 


namespace Ubriaco.Attributes
{
    public class VariableInfo : Attribute
    {
        public string? TypeInfo;

        public string? ForeignKey;

        public string? DefaultValue;

        public bool NoTimeZone;

        public VariableInfo() { }

        public VariableInfo(string? typeInfo, string? foreignKey = null)
        {
            TypeInfo = typeInfo;
            ForeignKey = foreignKey;
        }
    }
}
