using System;
using System.Collections.Generic;
using System.Text;

namespace Icarus
{
    internal class IcarusConfig
    {
		public string Version { get; set; }
		public string DatabaseIp { get; set; }
		public string SqlPassword { get; set; }
		public string SqlUsername { get; set; }
		public string DatabaseName { get; set; }
		public string Token { get; set; }
		public ulong GuildId { get; set; }
	}
}
