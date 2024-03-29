﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Icarus.Context.Models
{
    public class Nation
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public virtual List<Province> Provinces { get; set; } = new List<Province>();
        public virtual List<Modifier> Modifiers { get; set; } = new List<Modifier>();
    }
}
