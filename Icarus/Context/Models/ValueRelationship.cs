using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Icarus.Context.Models
{
    public class ValueRelationship
    {
        [Key]
        public int ValueRelationShipId { get; set; }
        public Value Origin { get; set; }
        public string OriginId { get; set; }
        public Value Target { get; set; }
        public string TargetId { get; set; }

        public float Factor { get; set; }
        public float Max { get; set; }
        public float Min { get; set; }
    }
}
