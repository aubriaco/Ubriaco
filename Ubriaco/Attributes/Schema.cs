/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
 


namespace Ubriaco.Attributes
{
    public class Schema : Attribute
    {
        public string Name;
        public Schema(string name)
        {
            Name = name;
        }
    }
}
