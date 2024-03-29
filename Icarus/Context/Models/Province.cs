﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Icarus.Context.Models
{
    public class Province
    {
        [Key]
        public int ProvinceId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public virtual List<Value> Values { get; set; } = new List<Value>();
        public virtual List<Modifier> Modifiers { get; set;} = new List<Modifier>();

        public int NationId { get; set; }
        public virtual Nation Nation { get; set; }
    }
}
