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
		private readonly TickService _tickService;

		public TestModule(DiscordSocketClient client, TickService tickService)
		{
			_client = client;
			_tickService = tickService;
		}

		[SlashCommand("test", "Test command please ignore")]
		public async Task GetTest()
		{
			await RespondAsync($"Tower was here");
		}
	}
}
