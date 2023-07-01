using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Icarus.Context.Models
{
    public class DiscordUser
    {
        [Key]
        public string DiscordId { get; set; }

        public virtual List<PlayerCharacter> Characters { get; set; }
        public bool CanUseAdminCommands { get; set; }

		public virtual List<StaffAction> CreatedStaffActions { get; set; }

		public virtual List<StaffAction> AssignedStaffActions { get; set; }
    }
}
