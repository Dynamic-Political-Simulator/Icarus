using Discord.WebSocket;
using System.Threading.Tasks;
using Icarus.Context;
using Discord.Interactions;
using Icarus.Services;
using System.Linq;
using System;
using Discord;
using System.Collections.Generic;

namespace Bailiff.Discord.Modules
{
	public class TestModule : InteractionModuleBase<SocketInteractionContext>
	{
		private readonly DiscordSocketClient _client;
		private readonly IcarusContext _dbcontext;
		private readonly TickService _tickService;
		private readonly GoogleSheetsService _gsheetsService;

		public TestModule(DiscordSocketClient client, IcarusContext dbcontext, TickService tickService, GoogleSheetsService gsheetsService)
		{
			_client = client;
			_dbcontext = dbcontext;
			_tickService = tickService;
			_gsheetsService = gsheetsService;
		}

		[SlashCommand("test", "Test command please ignore")]
		public async Task GetTest()
		{
			await RespondAsync($"Tower was here");
		}

		[SlashCommand("get-cell-test", "get a cell in google sheets")]
		public async Task GetCellTest(string spreadsheetID, string cellID)
		{
			await RespondAsync($"{_gsheetsService.GenerateContext(spreadsheetID).Get(cellID)[0][0]}");
		}

		[SlashCommand("update-cell-test", "get a cell in google sheets")]
		public async Task UpdateCellTest(string spreadsheetID, string cellID, string newVal)
		{
			await DeferAsync();
			_gsheetsService.GenerateContext(spreadsheetID).Update(cellID, new List<List<string>> { new List<string> { newVal } });
			await ModifyOriginalResponseAsync(x => x.Content = "Update made!");
		}
	}
}
