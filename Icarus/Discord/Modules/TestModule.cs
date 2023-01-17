using Discord.WebSocket;
using System.Threading.Tasks;
using Icarus.Context;
using Discord.Interactions;
using Icarus.Services;
using System.Linq;
using System;
using Discord;

namespace Bailiff.Discord.Modules
{
	public class TestModule : InteractionModuleBase<SocketInteractionContext>
	{
		private readonly DiscordSocketClient _client;
		private readonly IcarusContext _dbcontext;
		private readonly TickService _tickService;

		public TestModule(DiscordSocketClient client, IcarusContext dbcontext, TickService tickService)
		{
			_client = client;
			_dbcontext = dbcontext;
			_tickService = tickService;
		}

		[SlashCommand("test", "Test command please ignore")]
		public async Task GetTest()
		{
			await RespondAsync($"Tower was here");
		}

		[SlashCommand("force-tick", "Forces a tick")]
		public async Task ForceTick()
		{
			try
			{
				await DeferAsync();
				_tickService.ForceTick();
				await ModifyOriginalResponseAsync(x => x.Content = "A tick has been forced!");
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}

		[SlashCommand("set-tick", "Set the tick interval (in ms)")]
		public async Task SetTick(long interval)
		{
			await DeferAsync();
			GameState state = _dbcontext.GameStates.FirstOrDefault();

			state.TickInterval = interval;

			_dbcontext.GameStates.Update(state);
			await _dbcontext.SaveChangesAsync();
			await ModifyOriginalResponseAsync(x => x.Content = "Tick interval updated!");
		}

		[SlashCommand("alert-next-tick", "Send a DM notification when the next tick hits")]
		public async Task AlertNextTick(IUser user)
		{
			await DeferAsync();
			_tickService.NextTickEvent += async () => { await user.SendMessageAsync("A tick has ocurred!"); };
			await ModifyOriginalResponseAsync(x => x.Content = "Alert scheduled!");
		}
	}
}
