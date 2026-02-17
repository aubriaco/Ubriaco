/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

namespace Ubriaco.Models;

public class Diff
{
    public string? ModelName { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public object? Current { get; set; }
    public object? Other { get; set; }
}
