/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

namespace BridgeData.Attributes
{
    public class MSQLTable : Attribute
    {
        public string Name;
        public MSQLTable(string name)
        {
            Name = name;
        }
    }
}
