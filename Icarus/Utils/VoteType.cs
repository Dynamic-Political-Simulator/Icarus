using Castle.Core.Configuration;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Utils
{
	public enum VoteType
	{
		MAJORITY,
		TWOTHIRD,
		FPTP,
		TWOROUND,
		TWOROUNDFINAL // For the second part of the two round
	}
}
