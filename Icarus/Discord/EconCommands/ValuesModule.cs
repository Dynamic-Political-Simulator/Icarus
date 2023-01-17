using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Context.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Discord.EconCommands
{
    public class ValuesModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IcarusContext _icarusContext;
        private readonly DiscordSocketClient _client;

        public ValuesModule(IcarusContext icarusContext, DiscordSocketClient client)
        {
            _icarusContext = icarusContext;
            _client = client;
        }

        [SlashCommand("setValue","Set the specified Value to a certain Number.")]
        public async Task SetValue(string Value, float Number)
        {
            Value _Value = _icarusContext.Values.FirstOrDefault(v => v.Name== Value);
            if (_Value == null)
            {
                await RespondAsync($"{Value} is not a Valid Value");
            }
            if (Number < 0)
            {
                await RespondAsync("A Value can not be smaller than Zero");
            }

            _Value._Value = Number;

            await _icarusContext.SaveChangesAsync();

        }
    }
}
