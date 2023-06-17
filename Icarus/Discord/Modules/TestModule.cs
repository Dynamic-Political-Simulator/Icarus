using Discord.WebSocket;
using System.Threading.Tasks;
using Icarus.Context;
using Discord.Interactions;
using Icarus.Services;
using System.Linq;
using System;
using Discord;
using Icarus.Context.Models;
using System.Xml;

namespace Bailiff.Discord.Modules
{
	public class TestModule : InteractionModuleBase<SocketInteractionContext>
	{
		private readonly DiscordSocketClient _client;
		private readonly TickService _tickService;
        private readonly ValueManagementService _valueManagementService;

        public TestModule(DiscordSocketClient client, TickService tickService, ValueManagementService valueManagementService)
		{
			_client = client;
			_tickService = tickService;
			_valueManagementService = valueManagementService;
		}

		[SlashCommand("test", "Test command please ignore")]
		public async Task GetTest()
		{
			await RespondAsync($"Tower was here");
		}

		[SlashCommand("genrel", "What is says")]
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
