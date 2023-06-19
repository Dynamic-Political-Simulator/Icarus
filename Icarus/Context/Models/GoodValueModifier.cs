using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Icarus.Context.Models;

namespace Icarus.Context.Models
{
    public class GoodValueModifier
    {
        [Key]
        public int ID { get; set; }

        public float Modifier { get; set; }
        public float Decay { get; set; } = 0;

        public string ValueTag { get; set; }
        public virtual Good GoodWrapper { get; set; }
        public int GoodWrapperId { get; set; }
    }
}
