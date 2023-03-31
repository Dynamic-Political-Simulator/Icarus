using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Icarus.Context.Models;

namespace Icarus.Context.Models
{
    public class Value
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public float _Value { get; set; }
        public float RelationInducedChange { get; set; }
        public string ProvinceId { get; set; }
        public Province Province { get; set; }
    }
}
