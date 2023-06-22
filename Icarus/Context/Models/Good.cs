using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Icarus.Context.Models
{
    public class Good
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string TAG { get; set; }
        public string Description { get; set; }
        public float WealthMod { get; set; }

        public virtual List<GoodValueModifier> ValueModifiers { get; set; } = new List<GoodValueModifier>();

    }
}
