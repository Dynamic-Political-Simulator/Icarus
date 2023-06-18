using System.ComponentModel.DataAnnotations;

namespace Icarus.Context.Models
{
    public class GameState
    {
        [Key]
        public int GameStateId { get; set; }
        public long TickInterval { get; set; }
        public long LastTickEpoch { get; set; }
        public int Year { get; set; }
        public int? NationId { get; set; }
        public virtual Nation? Nation { get; set; }
    }
}
