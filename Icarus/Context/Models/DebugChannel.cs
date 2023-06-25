using System.ComponentModel.DataAnnotations;

namespace Icarus.Context.Models
{
    public class DebugChannel
    {
        [Key]
        public ulong ChannelId { get; set; }
    }
}
