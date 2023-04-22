using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Services;
using System.Threading.Tasks;

namespace Icarus.Discord.Modules
{
    public class GamestateModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ValueManagementService _valueManagementService;

        public GamestateModule(ValueManagementService valueManagementService)
        {
            _valueManagementService = valueManagementService;
        }

        [SlashCommand("starttestgame", "Starts a new Game")]
        public async Task StartTestGame()
        {
            using var db = new IcarusContext();
            //Some Code here to clean up the prior gameState

            GameState gameState = new()
            {
                //Propably wanna make this command parameters later
                Nation = new Nation()
                {
                    Name = "TestNation",
                    Description = "The Nation of TestNation is a wonderours place of testing"
                }
            };

            //For now just generating 10 dummy provinces called 0,1,2,3... eventually this should propably be read in from somewhere. Either an XML or maybe a google sheet
            for (int i = 0; i < 10; i++)
            {
                Province province = new Province()
                {
                    Name = i.ToString(),
                    Description = i.ToString(),
                    Nation = gameState.Nation
                };

                province.Values = _valueManagementService.GenerateValueTemplate();
                gameState.Nation.Provinces.Add(province);
                await _valueManagementService.GenerateValueRelationships(province.Values);
            }
            db.GameStates.Add(gameState);

            await db.SaveChangesAsync();
            await RespondAsync("Success!");
        }
    }
}