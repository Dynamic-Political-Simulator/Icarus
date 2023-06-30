using Discord.WebSocket;
using System.Threading.Tasks;
using Icarus.Context;
using Discord.Interactions;
using Icarus.Services;
using System.Linq;
using System;
using Discord;
using System.Collections.Generic;
using Icarus.Context.Models;
using System.Xml;
using Icarus.Discord.CustomPreconditions;

namespace Icarus.Discord.Modules
{
	public class TestModule : InteractionModuleBase<SocketInteractionContext>
	{
		private readonly DiscordSocketClient _client;
		private readonly TickService _tickService;
		private readonly GoogleSheetsService _gsheetsService;
		private readonly ValueManagementService _valueManagementService;

		public TestModule(DiscordSocketClient client, TickService tickService, GoogleSheetsService gsheetsService, ValueManagementService valueManagementService)
		{
			_client = client;
			_tickService = tickService;
			_gsheetsService = gsheetsService;
			_valueManagementService = valueManagementService;
		}

		[SlashCommand("test", "Test command please ignore")]
		public async Task GetTest()
		{
			await RespondAsync($"Tower was here");
		}

		[SlashCommand("get-cell-test", "get a cell in google sheets")]
		[RequireAdmin]
		public async Task GetCellTest(string spreadsheetID, string cellID)
		{
			await RespondAsync($"{_gsheetsService.GenerateContext(spreadsheetID).Get(cellID)[0][0]}");
		}

		[SlashCommand("update-cell-test", "get a cell in google sheets")]
        [RequireAdmin]
        public async Task UpdateCellTest(string spreadsheetID, string cellID, string newVal)
		{
			await DeferAsync();
			_gsheetsService.GenerateContext(spreadsheetID).Update(cellID, new List<List<string>> { new List<string> { newVal } });
			await ModifyOriginalResponseAsync(x => x.Content = "Update made!");
		}

		[SlashCommand("genrel", "What is says")]
        [RequireAdmin]
        public async Task RereadRelationShips()
		{
			string DataPath = @"./GameStateConfig.xml";
			XmlDocument Xmldata = new XmlDocument();
			Xmldata.Load(DataPath);
			XmlNode xmlNode = Xmldata.LastChild.SelectSingleNode("Nation");



			await _valueManagementService.GenerateValueRelationships(Xmldata.LastChild.SelectSingleNode("ValueRelationShips"));
			await RespondAsync("Done");
		}

		[SlashCommand("gengoods", "What it says")]
        [RequireAdmin]
        public async Task RereadGoods()
		{
			string DataPath = @"./GameStateConfig.xml";
			XmlDocument Xmldata = new XmlDocument();
			Xmldata.Load(DataPath);
			XmlNode xmlNode = Xmldata.LastChild.SelectSingleNode("Nation");



			await _valueManagementService.ReadGoodXml(Xmldata.LastChild.SelectSingleNode("Goods"));
			await RespondAsync("Done");
		}
	}
}
