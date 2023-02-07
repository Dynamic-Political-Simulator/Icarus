﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Icarus.Context.Models
{
    public class ValueRelationship
    {
        [Key]
        public int ValueRelationShipId { get; set; }
        public virtual Value Origin { get; set; }
        public int OriginId { get; set; }
        public virtual Value Target { get; set; }
        public int TargetId { get; set; }

        public float Factor { get; set; }
        public float Max { get; set; }
        public float Min { get; set; }
    }
}
