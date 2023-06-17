using Discord.Interactions;
using Discord.Interactions.Builders;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Discord.CustomPreconditions;
using Icarus.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Discord.Modules
{
    public class ActionModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DiscordSocketClient _client;
        private readonly ActionService _actionService;
        private readonly DiscordInteractionHelpers _interactionHelpers;

        public ActionModule(DiscordSocketClient client, ActionService actionService, DiscordInteractionHelpers interactionHelpers)
        {
            _client = client;
            _actionService = actionService;
            _interactionHelpers = interactionHelpers;
        }



        [SlashCommand("action-example", "action example please ignore")]
        [RequireProfile]
        [RequireTokenAmount(ActionTokenType.TestToken, 5, true)]
        public async Task ExampleAction()
        {
            using var db = new IcarusContext();

            var player = db.Users.First(u => u.DiscordId == Context.User.Id.ToString());

            var character = player.Characters.First(c => c.YearOfDeath == -1);

            var result = _actionService.ExampleAction(character);

            await RespondAsync(result);
        }

        [SlashCommand("give-favour", "Gives favours")]
        [RequireProfile]
        [RequireAdmin]
        public async Task GiveToken(SocketGuildUser mention, ActionTokenType tokenType, int amount)
        {
            if (amount > 7)
            {
                await RespondAsync("May not give a character more than seven favours of any type.");
                return;
            }

            using var db = new IcarusContext();

            var character = db.Characters.Include(c => c.Tokens)
                .FirstOrDefault(c => c.DiscordUserId == Context.User.Id.ToString() && c.YearOfDeath != -1);

            if (character == null)
            {
                await RespondAsync($"User {Context.User.Username} does not have an active character.");
                return;
            }

            var tokenEntry = character.Tokens.FirstOrDefault(t => t.TokenType == tokenType);

            if (tokenEntry == null)
            {
                var newTokenEntry = new CharacterToken()
                {
                    PlayerCharacterId = character.CharacterId,
                    TokenType = tokenType,
                    Amount = amount
                };

                character.Tokens.Add(newTokenEntry);

                tokenEntry = newTokenEntry;
            }

            if (tokenEntry.Amount > 7) tokenEntry.Amount =7;

            await RespondAsync("Added favours.");
        }
    }
}
