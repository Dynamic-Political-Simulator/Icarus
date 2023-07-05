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

        [SlashCommand("show-player-favours", "Lists the pinged player's favours.")]
        [RequireAdmin]
        public async Task ShowPlayerTokens(SocketGuildUser mention)
        {
            var tokens = await _actionService.GetAllTokensForProfileActiveCharacter(mention.Id.ToString());

            var embedBuilder = new EmbedBuilder();

            embedBuilder.WithTitle($"{mention.Username}'s Favours");

            if (tokens.Any())
            {
                var sb = new StringBuilder();

                foreach (var token in tokens)
                {
                    sb.AppendLine(token.TokenTypeId + ": " + token.Amount);
                }

                embedBuilder.WithDescription(sb.ToString());
            }
            else
            {
                embedBuilder.WithDescription($"{mention.Username} has no favours.");
            }

            await RespondAsync(embed: embedBuilder.Build());
        }

        [SlashCommand("favour-debt-list", "Lists the living characters with favour debt.")]
        public async Task ShowLivingCharactersWithDebt()
        {
            var embedBuilder = new EmbedBuilder();

            var sb  = new StringBuilder();

            await foreach (var line in _actionService.GetLivingCharactersWithDebt())
            {
                var user = await _client.GetUserAsync(ulong.Parse(line.DiscordId));
                sb.AppendLine(Format.Bold($"{user.Username} - {line.CharacterName}"));
                foreach (var debt in line.FavourDebtLines)
                {
                    sb.AppendLine($"{debt.FavourName}: {debt.Amount}");
                }
            }

            embedBuilder.WithTitle("Favour Debt");
            embedBuilder.WithDescription(sb.ToString());

            await RespondAsync(embed: embedBuilder.Build(), ephemeral: false);
        }

        [SlashCommand("my-favours", "Lists your tokens.")]
        [RequireProfile]
        public async Task ListMyTokens()
        {
            var tokens = await _actionService.GetAllTokensForProfileActiveCharacter(Context.User.Id.ToString());

            var embedBuilder = new EmbedBuilder();

            embedBuilder.WithTitle("Your Favours");

            if (tokens.Any())
            {
                var sb = new StringBuilder();

                foreach (var token in tokens)
                {
                    sb.AppendLine(token.TokenTypeId + ": " + token.Amount);
                }

                embedBuilder.WithDescription(sb.ToString());
            }
            else
            {
                embedBuilder.WithDescription("You have no favours.");
            }
            
            await RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
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
            var result = await _actionService.GiveToken(mention, tokenType, amount);

            await RespondAsync(result);
        }

        [SlashCommand("list-favour-types", "Lists all favour types present in the database")]
        public async Task ListTokenTypes()
        {
            var types = _actionService.GetTokenTypes();

            var embedBuilder = new EmbedBuilder()
                .WithTitle("Favour types.")
                .WithDescription(types.Aggregate((current, next) => current + "\n" + next));

            await RespondAsync(embed: embedBuilder.Build());
        }

        [SlashCommand("create-favour", "Creates a new favour type.")]
        [RequireAdmin]
        public async Task CreateTokenType(string tokenName)
        {
            await RespondAsync(await _actionService.NewTokenType(tokenName));
        }
    }
}
