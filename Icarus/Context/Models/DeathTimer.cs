using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Context.Models
{
    public class DeathTimer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string CharacterId { get; set; }

        //Initially null, set when the actual kill command is used and it goes to graveyard.
        public DateTime? TimeKilled { get; set; }
    }
}
