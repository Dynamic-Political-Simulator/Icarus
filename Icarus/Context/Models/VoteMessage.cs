using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Icarus.Context.Models
{
	public class VoteMessage
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public ulong MessageId { get; set; }
		public ulong CreatorId { get; set; }
		public ulong ChannelId { get; set; }
		public int Type { get; set; }
		public long EndTime { get; set; } // In FileTime
		public long TimeSpan { get; set; } // In Ticks
	}
}
