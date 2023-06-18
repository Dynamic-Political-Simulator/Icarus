using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Discord.CustomPreconditions;
using Icarus.Services;
using Microsoft.EntityFrameworkCore;
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

        [SlashCommand("my-favours", "Lists your tokens.")]
        [RequireProfile]
        public async Task ListMyTokens()
        {
            var tokens = _actionService.GetAllTokensForProfileActiveCharacter(Context.User.Id.ToString());

            var embedBuilder = new EmbedBuilder();

            embedBuilder.WithTitle("Your Tokens");

            var sb = new StringBuilder();

            foreach (var token in tokens)
            {
                sb.AppendLine(token.TokenTypeId + ": " + token.Amount);
            }

            embedBuilder.WithDescription(sb.ToString());

            await RespondAsync(embed: embedBuilder.Build());
        }

        [SlashCommand("action-example", "action example please ignore")]
        [RequireProfile]
        [RequireTokenAmount("TestToken", 5, true)]
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
        public async Task GiveToken(SocketGuildUser mention, string tokenType, int amount)
        {
            var result = await _actionService.GiveToken(mention, tokenType, amount, Context.User.Id.ToString());

            await RespondAsync(result);
        }

        [SlashCommand("list-token-types", "Lists all token types present in the database")]
        public async Task ListTokenTypes()
        {
            var types = _actionService.GetTokenTypes();

            var embedBuilder = new EmbedBuilder()
                .WithTitle("Token types.")
                .WithDescription(types.Aggregate((current, next) => current + "\n" + next));

            await RespondAsync(embed: embedBuilder.Build());
        }

        [SlashCommand("create-token", "Creates a new token type.")]
        [RequireAdmin]
        public async Task CreateTokenType(string tokenName)
        {
            await RespondAsync(await _actionService.NewTokenType(tokenName));
        }
    }
}
