﻿using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Icarus.Context.Models;

namespace Icarus.Context.Models
{
    public class Modifier
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; }
        //public float Decay { get; set; }
        public ModifierType Type { get; set; }
        public bool isGood { get; set; } = false;
        public virtual List<ValueModifier> Modifiers { get; set; } = new List<ValueModifier>();

    }

    public enum ModifierType
    {
        Permanent,
        Temporary,
        Decaying
    }
}
