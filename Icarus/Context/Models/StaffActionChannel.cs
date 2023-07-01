using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Icarus.Context.Models
{
	public class StaffActionChannel
	{
		[Key]
		public string ChannelId { get; set; }
		public string ServerId { get; set; }
	}
}