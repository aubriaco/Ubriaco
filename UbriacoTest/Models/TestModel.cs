/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ubriaco.Attributes;
using Ubriaco.Models;

namespace UbriacoTest.Models
{
    [Table("tests")]
    internal class TestModel : BaseModel
    {
        public string? TestVar1 { get; set; }
        public int TestNumber2 { get; set; }
        public double TestDouble3 { get; set; }
    }
}
