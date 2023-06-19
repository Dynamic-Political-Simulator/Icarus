using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Utils;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Timers;

namespace Icarus.Services
{
	/// <summary>
	/// A service that manages the tick system. Fires a tick event at a specific interval defined in the GameState stored in the DB.
	/// </summary>
	public class TickService
	{
		/// <summary>
		/// The function delegate for the TickEvent handler.
		/// </summary>
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

		public TickService()
		{
			StartTickCheck();
		}

		/// <summary>
		/// Starts the timer that calls the tick checking function.
		/// </summary>
		private void StartTickCheck()
		{
			TickEvent += CallSingleTickHandlers; // Subscribe the NextTickEvent invoker to the TickEvent

            var icarusConfig = ConfigFactory.GetConfig();

            var timer = new Timer(icarusConfig.TickResolution); // The timer will fire every N ms (defined in config)
			timer.Elapsed += TickCheck;
			timer.AutoReset = true; // Loop the timer once it elapses
			timer.Enabled = true;
		}

		/// <summary>
		/// Fires the NextTickEvent event and clears all of its listeners.
		/// </summary>
		private void CallSingleTickHandlers()
		{
			NextTickEvent?.Invoke();
			NextTickEvent = null; // Clear all subscribers as the event has passed
		}

		/// <summary>
		/// Checks whether enough time has elapsed for an another tick, called every time the Timer defined in StartTickCheck elapses.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		private void TickCheck(object source, ElapsedEventArgs e)
		{
			using var db = new IcarusContext();
			var state = db.GameStates.FirstOrDefault();

			if (state.TickInterval >= 0)
			{
				long currentEpoch = DateTime.Now.ToFileTimeUtc();
				// If the difference between current epoch and saved epoch exceeds the interval (defined in ms), a tick has ocurred.
				if (currentEpoch - state.LastTickEpoch >= state.TickInterval)
				{
					TickEvent?.Invoke();
					state.LastTickEpoch = currentEpoch;
					GC.Collect();
				}

				db.GameStates.Update(state);
				db.SaveChanges();
			}
		}

		/// <summary>
		/// Forces a tick without checking whether the interval has passed.
		/// </summary>
		public void ForceTick()
		{
            using var db = new IcarusContext();
            GameState state = db.GameStates.FirstOrDefault();

			long currentEpoch = DateTime.Now.ToFileTimeUtc();
			TickEvent?.Invoke();
			state.LastTickEpoch = currentEpoch;

			db.GameStates.Update(state);
			db.SaveChanges();
		}
	}
}
