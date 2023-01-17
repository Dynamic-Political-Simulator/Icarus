using System.ComponentModel.DataAnnotations;

namespace Icarus.Context
{
	// Data entry for the Game State, containing info like tick interval, last tick, etc.
	public class GameState
	{
		[Key]
		public int GameStateId { get; set; } = 1;
		public long TickInterval { get; set; }
		public long LastTickEpoch { get; set; }
	}
}