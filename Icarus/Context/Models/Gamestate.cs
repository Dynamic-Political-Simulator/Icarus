using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Icarus.Context.Models
{
    public class Gamestate
    {
        [Key]
        public int Id { get; set; }
        public Nation Nation { get; set; }
        //public string NationID { get; set; }
    }
}
