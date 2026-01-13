/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
 


namespace Ubriaco.Attributes
{
    public class Table : Attribute
    {
        public string Name;
        public Table(string name)
        {
            Name = name;
        }
    }
}
