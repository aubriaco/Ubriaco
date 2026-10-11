/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

namespace Ubriaco.Attributes;

public class FKey : Attribute
{
    public Type ModelType { get; set; }
    public string PropertyName { get; set; }
    public FKey(Type modelType, string propertyName)
    {
        ModelType = modelType;
        PropertyName = propertyName;
    }
}