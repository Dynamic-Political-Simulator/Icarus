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
        public string Description { get; set; }
        public string TAG { get; set; }
        public float CurrentValue { get; set; }
        public float BaseBalue { get; set; }
        //public float GoalValue { get; set; }
        public int ProvinceId { get; set; }
        public virtual Province Province { get; set; }
    }
}
