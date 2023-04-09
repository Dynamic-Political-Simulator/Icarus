using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Icarus.Context.Models
{
    public class GameState
    {
        [Key]
        public int GameStateId { get; set; } = 1;
        public long TickInterval { get; set; }
        public long LastTickEpoch { get; set; }

        public int NationId { get; set; }
        public Nation Nation { get; set; }
    }
}
