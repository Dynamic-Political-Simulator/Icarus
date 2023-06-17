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
        public string OriginTag { get; set; }
        public string TargetTag { get; set; }
        public float Weight { get; set; }
    }
}
