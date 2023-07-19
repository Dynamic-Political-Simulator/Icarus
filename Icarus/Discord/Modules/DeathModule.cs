using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using Icarus.Discord.CustomPreconditions;
using Icarus.Services;

namespace Icarus.Discord.Modules
{
    public class DeathModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DeathService _deathService;

        public DeathModule(DeathService deathService)
        {
            _deathService = deathService;
        }

        [SlashCommand("kill-character", "Kills the active character of the mentioned user.")]
        [RequireAdmin]
        public async Task KillCharacter(SocketGuildUser mention)
        {
            await DeferAsync();

            var result = await _deathService.KillCharacter(mention.Id.ToString());

            await FollowupAsync(result);
        }
    }
}
