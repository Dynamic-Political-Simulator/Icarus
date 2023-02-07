using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Discord.CustomPreconditions;
using Icarus.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Discord.Modules
{
    [Group("token", "Token commands")]
    public class TokenModule: InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DiscordSocketClient _client;
        private readonly IcarusContext _dbContext;
        private readonly TokenService _tokenService;

        public TokenModule(DiscordSocketClient client, IcarusContext dbContext, TokenService tokenService)
        {
            _client = client;
            _dbContext = dbContext;
            _tokenService = tokenService;
        }

        [SlashCommand("add", "Add tokens to the pinged user's active character")]
        [RequireStaff()]
        public async Task AddTokensToCharacter(IUser user, [Autocomplete(typeof(ActionTokenAutoCompleteHandler))] ActionTokenType tokenType, int amount)
        {
            var discordUser = _dbContext.Users.SingleOrDefault(u => u.DiscordId == user.Id.ToString());

            var character = discordUser.Characters.SingleOrDefault(c => c.YearOfDeath == -1);

            _tokenService.AddTokensToCharacter(character, tokenType, amount);

            await RespondAsync($"Added {amount} Tokens of type {tokenType.ToString()} to {character.CharacterName}.");
        }

        public class ActionTokenAutoCompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                // Create a collection with suggestions for autocomplete
                IEnumerable<AutocompleteResult> results = new List<AutocompleteResult>();

                var values = Enum.GetValues(typeof(ActionTokenType)).Cast<ActionTokenType>();

                foreach (var v in values)
                {
                    results.Append(new AutocompleteResult(v.ToString(), v));
                }

                // max - 25 suggestions at a time (API limit)
                return AutocompletionResult.FromSuccess(results.Take(25));
            }
        }

        public async Task RemoveTokensFromCharacter(IUser user, [Autocomplete(typeof(ActionTokenAutoCompleteHandler))] ActionTokenType tokenType, int amount)
        {
            var discordUser = _dbContext.Users.SingleOrDefault(u => u.DiscordId == user.Id.ToString());

            var character = discordUser.Characters.SingleOrDefault(c => c.YearOfDeath == -1);

            var result = await _tokenService.RemoveTokensFromCharacter(character, tokenType, amount);

            if (result == -1)
            {
                await RespondAsync($"Removed {amount} Tokens of type {tokenType.ToString()} from {character.CharacterName}.");
            }
            else
            {
                await RespondAsync($"Could not remove {amount} tokens, character only had {result} tokens so only {result} tokens were deducted.");
            }
        }
    }
}
