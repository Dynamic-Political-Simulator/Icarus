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

        public float Modifier { get; set; }
        public float Decay { get; set; }

        public string ValueName { get; set; }
        public Modifier ModifierWrapper { get; set; }
        public int ModifierWrapperId { get; set; }
    }
}
