using System;
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

        public float WealthMod { get; set; } = 1f;

        public Level Level { get; set; } = Level.Miniscule;

    }

    public enum ModifierType
    {
        Permanent,
        Temporary,
        Decaying
    }

    public enum Level
    {
        Miniscule, //0.25
        Small, //0.5
        Normal, //1
        Large, //2
        Massive //4
    }
}
