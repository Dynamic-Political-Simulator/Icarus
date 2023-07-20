using Discord.Interactions;
using Icarus.Discord.CustomPreconditions;
using Icarus.Services;
using System.Threading.Tasks;

namespace Icarus.Discord.Modules
{
    public class AgeModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AgeService _ageService;

        public AgeModule(AgeService ageService)
        {
            _ageService = ageService;
        }

        [SlashCommand("toggle-aging", "Toggles whether or not characters age.")]
        [RequireAdmin]
        public async Task ToggleAging()
        {
            var result = await _ageService.ToggleAging();

            await RespondAsync($"AgingEnabled set to {result}.");
        }
    }
}
