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
    public class ActionModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DiscordSocketClient _client;
        private readonly ActionService _actionService;

        public ActionModule(DiscordSocketClient client, ActionService actionService)
        {
            _client = client;
            _actionService = actionService;
        }

        [SlashCommand("action example", "action example please ignore")]
        [RequireTokenAmount(ActionTokenType.TestToken, 5, true)]
        public async Task ExampleAction()
        {
            using var db = new IcarusContext();

            var player = db.Users.First(u => u.DiscordId == Context.User.Id.ToString());

            var character = player.Characters.First(c => c.YearOfDeath == -1);

            var result = _actionService.ExampleAction(character);

            await RespondAsync(result);
        }
    }
}
