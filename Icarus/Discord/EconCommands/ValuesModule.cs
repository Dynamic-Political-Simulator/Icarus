using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Services;
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
        private readonly ValueManagementService _valueManagementService;

        public ValuesModule(IcarusContext icarusContext, DiscordSocketClient client, ValueManagementService valueManagementService)
        {
            _icarusContext = icarusContext;
            _client = client;
            _valueManagementService = valueManagementService;
        }

        [SlashCommand("setValue","Set the specified Value to a certain Number.")]
        public async Task SetValue(string ProvinceName,string ValueName, float Number)
        {
            if (Number < 0)
            {
                await RespondAsync("A Value can not be smaller than Zero");
            }
            Province province = _icarusContext.Provinces.FirstOrDefault(p => p.Name == ProvinceName);
            if (province == null)
            {
                await RespondAsync($"{ProvinceName} was not found!");
            }
            Value Value = province.Values.FirstOrDefault(v => v.Name == ValueName);
            if (Value == null)
            {
                await RespondAsync($"{ValueName} is not a Valid Value");
            }
            

            Value._Value = Number;

            await _icarusContext.SaveChangesAsync();
            await RespondAsync("Success!");

        }

        [SlashCommand("showValues", "Show Values of a Province")]
        public async Task ShowValues(string ProvinceName)
        {
            Province province = _icarusContext.Provinces.FirstOrDefault(p => p.Name == ProvinceName);
            if (province == null)
            {
                await RespondAsync($"{ProvinceName} was not found!");
            }

            StringBuilder stringBuilder= new StringBuilder();
            stringBuilder.AppendLine($"Value Name : Value");
            foreach (Value v in province.Values)
            {   
                if (_valueManagementService.GetValueChange(v) > 0)
                {
                    stringBuilder.AppendLine($"{v.Name} : {v._Value}(+{_valueManagementService.GetValueChange(v)})");
                }
                else
                {
                    stringBuilder.AppendLine($"{v.Name} : {v._Value}({_valueManagementService.GetValueChange(v)})");
                }
            }
        }
    }
}
