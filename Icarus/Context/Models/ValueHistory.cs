using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Icarus.Context.Models
{
    public class ValueHistory
    {
        [Key]
        public int Id { get; set; }
        public float Height { get; set; }
        public float Goal { get; set; }
        public float Change { get; set; }

        public virtual Value Value { get; set; }

        public int ValueId { get; set; }
    }
}
