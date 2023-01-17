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
	public class TickService
	{
		private readonly IConfiguration Configuration;
		// TODO: Copied this from Babel, was this the right way of working with the Database Context?
		private readonly IcarusContext _dbcontext;

		public delegate void TickHandler();

		/// <summary>
		/// This event is called every time a tick occurs.
		/// </summary>
		public event TickHandler TickEvent;

		/// <summary>
		/// This event is called when a <c>TickEvent</c> occurs, it is cleared once it has been called.
		/// This is useful for scenarios where you wish to subscribe to the event once.
		/// </summary>
		public event TickHandler NextTickEvent;

		public TickService(IConfiguration configuration, IcarusContext dbcontext)
		{
			Configuration = configuration;
			_dbcontext = dbcontext;
			StartTickCheck();
		}

		private void StartTickCheck()
		{
			var icarusConfig = new IcarusConfig();
			Configuration.GetSection("IcarusConfig").Bind(icarusConfig);

			TickEvent += CallSingleTickHandlers;

			var timer = new Timer(icarusConfig.TickResolution);
			timer.Elapsed += TickCheck;
			timer.AutoReset = true;
			timer.Enabled = true;
		}

		private void CallSingleTickHandlers()
		{
			NextTickEvent?.Invoke();
			NextTickEvent = null; // Clear all subscribers as the event has passed
		}

		private void TickCheck(Object source, ElapsedEventArgs e)
		{
			GameState state = _dbcontext.GameStates.FirstOrDefault();

			if (state.TickInterval >= 0)
			{
				long currentEpoch = DateTime.Now.ToFileTimeUtc();
				if (currentEpoch - state.LastTickEpoch >= state.TickInterval)
				{
					TickEvent?.Invoke();
					state.LastTickEpoch = currentEpoch;
				}

				_dbcontext.GameStates.Update(state);
				_dbcontext.SaveChanges();
			}
		}

		public void ForceTick()
		{
			GameState state = _dbcontext.GameStates.FirstOrDefault();

			long currentEpoch = DateTime.Now.ToFileTimeUtc();
			TickEvent?.Invoke();
			state.LastTickEpoch = currentEpoch;

			_dbcontext.GameStates.Update(state);
			_dbcontext.SaveChanges();
		}
	}
}
