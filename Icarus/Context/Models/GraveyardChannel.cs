using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Context.Models
{
    public class GraveyardChannel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong ChannelId { get; set; }
    }
}
