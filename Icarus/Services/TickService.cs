using Discord;
using Discord.WebSocket;
using Icarus.Context;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Icarus.Services
{
	public delegate void TickHandler();
	public class TickService
	{
		private readonly IConfiguration Configuration;
		// TODO: Copied this from Babel, was this the right way of working with the Database Context?
		private readonly IcarusContext _dbcontext;

		public event TickHandler TickEvent;

		public TickService(IConfiguration configuration, IcarusContext dbcontext)
		{
			Configuration = configuration;
			_dbcontext = dbcontext;
			StartTickCheck();
		}

		private void StartTickCheck()
		{
			var timer = new Timer(Configuration.GetValue<int>("TickResolution"));
			timer.Elapsed += TickCheck;
			timer.Enabled = true;
		}

		private void TickCheck(Object source, ElapsedEventArgs e)
		{
			GameState state = _dbcontext.GameStates.FirstOrDefault();
			// If the Context has no GameState, generate a new one.
			state ??= new GameState
			{
				LastTickEpoch = 0,
				TickInterval = -1 // A TickInterval of -1 means that it will never tick
			};

			if (state.TickInterval >= 0)
			{
				long currentEpoch = DateTime.Now.ToFileTimeUtc();
				if (currentEpoch - state.LastTickEpoch >= state.TickInterval)
				{
					TickEvent.Invoke();
					state.LastTickEpoch = currentEpoch;
				}
			}

			_dbcontext.GameStates.Update(state);
			_dbcontext.SaveChanges();
		}
	}
}
