using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Discord
{
    public class GamestateCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IcarusContext _icarusContext;
        private readonly DiscordSocketClient _client;
        private readonly ValueManagementService _valueManagementService;

        public GamestateCommands(IcarusContext icarusContext, DiscordSocketClient client, ValueManagementService valueManagementService)
        {
            _icarusContext = icarusContext;
            _client = client;
            _valueManagementService = valueManagementService;
        }

        [SlashCommand("starttestgame","Starts a new Game")]
        public async Task startTestGame()
        {
            //Some Code here to clean up the prior gameState

            //
            Gamestate gamestate = new Gamestate();
            //Propably wanna make this command parameters later
            gamestate.Nation = new Nation() {
                Name = "TestNation",
                Description = "The Nation of TestNation is a wonderours place of testing"
            };
            
            //The Gamestate must be added to the DB somehow. Will hold of till merge with other gamestate changes.

            //For now just generating 10 dummy provinces called 0,1,2,3... eventually this should propably be read in from somewhere. Either an XML or maybe a google sheet
            for (int i = 0;i < 10; i++)
            {
                Province province= new Province() {
                    Name = i.ToString(),
                    Description = i.ToString(),
                    Nation = gamestate.Nation
                };

                province.Values = _valueManagementService.GenerateValueTemplate();
                gamestate.Nation.Provinces.Add(province);
                await _valueManagementService.GenerateValueRelationships(province.Values);
            }
            _icarusContext.Gamestates.Add(gamestate);

            await _icarusContext.SaveChangesAsync();
            await RespondAsync("Success!");
        }
    }
}
