using Discord.Interactions;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Services;
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
        private readonly IcarusContext _dbContext;
        private readonly ActionService _actionService;

        public ActionModule(DiscordSocketClient client, ActionService actionService)
        {
            _client = client;
            _actionService = actionService;
        }

        [SlashCommand("action example", "action example please ignore")]
        public async Task ExampleAction()
        {
            var player = _dbContext.Users.First(u => u.DiscordId == Context.User.Id.ToString());

            var character = player.Characters.First(c => c.YearOfDeath == -1);

            var result = _actionService.ExampleAction(character);

            if (result) 
            {
                await RespondAsync($"Sufficient Kromer.");
            }
            else
            {
                await RespondAsync($"Insufficient Kromer.");
            }
        }
    }
}
