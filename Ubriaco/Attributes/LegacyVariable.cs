/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
 


namespace Ubriaco.Attributes
{
    public class LegacyVariable : Attribute
    {
        public string RealVar;
        public Type RealType;

        public LegacyVariable(string realVar, Type realType = null)
        {
            RealVar = realVar;
            RealType = realType;
        }
    }
}
