using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Context.Models
{
    public class GroupOfInterest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ulong DiscordRoleId { get; set; }
        public virtual List<PlayerCharacter> Characters { get; set; }
    }
}
