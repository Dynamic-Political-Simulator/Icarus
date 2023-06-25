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
    public class DebugModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DebugService _debugService;

        public DebugModule(DebugService debugService)
        {
            _debugService = debugService;
        }

        [SlashCommand("add-debug-channel", "Adds a debug channel")]
        [RequireAdmin]
        public async Task AddChannel()
        {
            await _debugService.AddChannel(Context.Channel.Id);
            await ReplyAsync("Debug channel added.");
        }
    }
}
