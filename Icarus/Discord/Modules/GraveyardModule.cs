using Discord.Interactions;
using Icarus.Discord.CustomPreconditions;
using Icarus.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Discord.Modules
{
    public class GraveyardModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly GraveyardService _graveyardService;

        public GraveyardModule(GraveyardService graveyardService) 
        {
            _graveyardService = graveyardService;
        }

        [SlashCommand("add-graveyard", "Adds the channel as a graveyard.")]
        public async Task AddGraveyard()
        {
            var channelId = Context.Channel.Id;

            var result = await _graveyardService.AddGraveyardChannel(channelId);

            await RespondAsync(result);
        }
    }
}
