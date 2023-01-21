using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Icarus.Context.Models;

namespace Icarus.Context.Models
{
    public class ValueModifier
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public float Modifier { get; set; }
        public int Duration { get; set; }
        public int Decay { get; set; }
        public ModifierType Type { get; set; }

        public string ValueId { get; set; }
        public Value Value { get; set; }
    }

    public enum ModifierType
    {
        Permanent,
        Temporary,
        Decaying
    }
}
