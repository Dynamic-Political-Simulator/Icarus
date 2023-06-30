using Discord.WebSocket;
using System.Threading.Tasks;
using Icarus.Context;
using Discord.Interactions;
using Icarus.Services;
using System.Linq;
using System;
using Discord;
using Icarus.Context.Models;

namespace Icarus.Discord.Modules
{
	[Group("tick", "Tick commands")]
	public class TickModule : InteractionModuleBase<SocketInteractionContext>
	{
		private readonly DiscordSocketClient _client;
		private readonly TickService _tickService;

		public TickModule(DiscordSocketClient client, TickService tickService)
		{
			_client = client;
			_tickService = tickService;
		}

		[Group("configure", "Tick configuration commands")]
		public class TickConfigModule : InteractionModuleBase<SocketInteractionContext>
		{
			private readonly DiscordSocketClient _client;
			private readonly TickService _tickService;

			public TickConfigModule(DiscordSocketClient client, TickService tickService)
			{
				_client = client;
				_tickService = tickService;
			}

			[SlashCommand("force", "Forces a tick")]
			public async Task ForceTick()
			{
				await DeferAsync();
				_tickService.ForceTick();
				await ModifyOriginalResponseAsync(x => x.Content = "A tick has been forced!");
			}

			[SlashCommand("set", "Set the tick interval")]
			public async Task SetTick(long milliseconds = 0, int seconds = 0, int minutes = 0, int hours = 0)
			{
                using var db = new IcarusContext();

                long interval = (((hours * 60) + minutes) * 60 + seconds) * 1000 + milliseconds; // Sum up all the defined time units
				await DeferAsync();

				if (interval <= 0)
				{
					await ModifyOriginalResponseAsync(x => x.Content = "Tick interval cannot be negative or zero!");
					return;
				}

				// Retrieve the GameState object, update the TickInterval and save it.
				GameState state = db.GameStates.FirstOrDefault();
				state.TickInterval = interval;
				db.GameStates.Update(state);
				await db.SaveChangesAsync();

				await ModifyOriginalResponseAsync(x => x.Content = "Tick interval updated!");
			}
		}

		[SlashCommand("alert", "Send a DM notification when the next tick hits")]
		public async Task AlertNextTick()
		{
			await DeferAsync();
			_tickService.NextTickEvent += async () => { await Context.User.SendMessageAsync("A tick has ocurred!"); };
			await ModifyOriginalResponseAsync(x => x.Content = "Alert scheduled!");
		}
	}
}
