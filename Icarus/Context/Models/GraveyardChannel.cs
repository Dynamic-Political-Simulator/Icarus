using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Context.Models
{
    public class GraveyardChannel
    {
        [Key]
        public ulong ChannelId { get; set; }
    }
}
