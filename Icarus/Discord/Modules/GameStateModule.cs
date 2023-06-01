﻿using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Services;
using System.Linq;
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
            

            GameState state = db.GameStates.FirstOrDefault();
            await _valueManagementService.ReadGameStateConfig(state);

            await db.SaveChangesAsync();
            await RespondAsync("Success!");
        }
    }
}