using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Icarus.Context.Models
{
    public class DiscordUser
    {
        [Key]
        public string DiscordId { get; set; }

        public List<PlayerCharacter> Characters { get; set; }
        public bool CanUseAdminCommands { get; set; }
    }
}
