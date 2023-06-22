using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Icarus.Discord.EconCommands
{
    public class ValuesModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DiscordSocketClient _client;
        private readonly ValueManagementService _valueManagementService;

        public ValuesModule(DiscordSocketClient client, ValueManagementService valueManagementService)
        {
            _client = client;
            _valueManagementService = valueManagementService;
        }

        [SlashCommand("setvalue","Set the specified Value to a certain Number.")]
        public async Task SetValue(string ProvinceName,string ValueName, float Number)
        {
            using var db = new IcarusContext();

            if (Number < 0)
            {
                await RespondAsync("A Value can not be smaller than Zero");
            }
            Province province = db.Provinces.FirstOrDefault(p => p.Name == ProvinceName);
            if (province == null)
            {
                await RespondAsync($"{ProvinceName} was not found!");
            }
            Value Value = province.Values.FirstOrDefault(v => v.Name == ValueName);
            if (Value == null)
            {
                await RespondAsync($"{ValueName} is not a Valid Value");
            }
            

            Value.CurrentValue = Number;

            await db.SaveChangesAsync();
            await RespondAsync("Success!");

        }

        [SlashCommand("showvalues", "Show Values of a Province")]
        public async Task ShowValues(string ProvinceName)
        {
            using var db = new IcarusContext();

            Province province = db.Provinces.FirstOrDefault(p => p.Name == ProvinceName);
            if (province == null)
            {
                await RespondAsync($"{ProvinceName} was not found!");
            }

            StringBuilder stringBuilder= new StringBuilder();
            stringBuilder.AppendLine($"Value Name : Value");
            foreach (Value v in province.Values)
            {   
                if (_valueManagementService.GetValueChange(v) >= 0)
                {
                    stringBuilder.AppendLine($"{v.Name} : {v.CurrentValue}(+{_valueManagementService.GetValueChange(v)};{_valueManagementService.GetValueGoal(v)})");
                }
                else
                {
                    stringBuilder.AppendLine($"{v.Name} : {v.CurrentValue}({_valueManagementService.GetValueChange(v)};{_valueManagementService.GetValueGoal(v)})");
                }
            }
            await RespondAsync(stringBuilder.ToString());
        }

        [SlashCommand("showprovince","Show general Province Info")]
        public async Task ShowProvince(string ProvinceName)
        {
            using var db = new IcarusContext();

            Province province = db.Provinces.FirstOrDefault(p => p.Name == ProvinceName);
            if (province == null)
            {
                await RespondAsync($"{ProvinceName} was not found!");
                return;
            }

            EmbedBuilder emb = new EmbedBuilder() 
            {
                Title = province.Name,
                Description= province.Description,
            };

            StringBuilder GoodsList = new StringBuilder();
            foreach (Modifier good in province.Modifiers.Where(m => m.isGood == true))
            {
                GoodsList.Append(good.Name+", ");
            }
            if(province.Modifiers.FirstOrDefault(m => m.isGood == true) == null)
            {
                GoodsList.Append("None");
            }
            

            StringBuilder ModifierList = new StringBuilder();
            foreach(Modifier modifier in province.Modifiers.Where(m => m.isGood == false))
            {
                ModifierList.Append(modifier.Name+", ");
            }
            if (province.Modifiers.FirstOrDefault(m => m.isGood != true) == null)
            {
                GoodsList.Append("None");
            }

            StringBuilder stringBuilder = new StringBuilder();
            string format = "{0,20} {1,8} {2,8} {3,8}";
            stringBuilder.AppendLine(String.Format(format, "Value", "Height", "Goal", "Projected Change"));
            foreach (Value v in province.Values)
            {
                if (_valueManagementService.GetValueChange(v) >= 0)
                {

                    //stringBuilder.AppendLine($"{v.Name} : {v.CurrentValue}(+{_valueManagementService.GetValueChange(v)};{_valueManagementService.GetValueGoal(v)})");
                    stringBuilder.AppendLine(String.Format(format, v.Name, v.CurrentValue, _valueManagementService.GetValueGoal(v), "+"+_valueManagementService.GetValueChange(v)));
                }
                else
                {
                    stringBuilder.AppendLine(String.Format(format, v.Name, v.CurrentValue, _valueManagementService.GetValueGoal(v), _valueManagementService.GetValueChange(v)));
                }
            }

            try {
                stringBuilder.ToString();
                emb.AddField("Goods", GoodsList.ToString());

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            
            //emb.AddField("Goods", GoodsList.ToString());

            emb.AddField("Modifier", ModifierList.ToString());
            emb.AddField("Values", stringBuilder.ToString());

            await RespondAsync(embed: emb.Build());
        }

        /*
        //Local Modifiers
        [SlashCommand("CreateLocalStaticModifier", "Creates a new permanent Modifier in a specific Province")]
        public async Task CreateLocalStaticModifier(string ProvinceName, string ValueName, string ModifierName, string Description, string Modifier)
        {
            ValueModifier valueModifier = new ValueModifier()
            {
                Name = ModifierName,
                Description = Description,
                Modifier = float.Parse(Modifier),
                Type = ModifierType.Permanent,
                Locality = Locality.Local
            };
            Province p = _icarusContext.Provinces.FirstOrDefault(p=>p.Name == ProvinceName);
            if (p == null)
            {
                await RespondAsync("Province not Found");
            }
            Value v = p.Values.FirstOrDefault(v=>v.Name == ValueName);
            if (v == null)
            {
                await RespondAsync("Value not Found");
            }
            v.Modifiers.Add(valueModifier);
            await _icarusContext.SaveChangesAsync();
            await RespondAsync($"Created new permanent local Modifier at {p.Name} affecting {v.Name} by {valueModifier.Modifier}");
        }

        [SlashCommand("CreateLocalTemporaryModifier", "Creates a new temporary Modifier in a specific Province")]
        public async Task CreateLocalTemporaryModifier(string ProvinceName, string ValueName, string ModifierName, string Description, string Modifier, string Ticks)
        {
            ValueModifier valueModifier = new ValueModifier()
            {
                Name = ModifierName,
                Description = Description,
                Modifier = float.Parse(Modifier),
                Type = ModifierType.Temporary,
                Duration = int.Parse(Ticks),
                Locality = Locality.Local
            };
            Province p = _icarusContext.Provinces.FirstOrDefault(p => p.Name == ProvinceName);
            if (p == null)
            {
                await RespondAsync("Province not Found");
            }
            Value v = p.Values.FirstOrDefault(v => v.Name == ValueName);
            if (v == null)
            {
                await RespondAsync("Value not Found");
            }
            v.Modifiers.Add(valueModifier);
            _icarusContext.Modifiers.Add(valueModifier);
            await _icarusContext.SaveChangesAsync();
            await RespondAsync($"Created new temporary local Modifier at {p.Name} affecting {v.Name} by {valueModifier.Modifier} lasting {valueModifier.Duration} Ticks");
        }

        [SlashCommand("CreateLocalDecayingModifier", "Creates a new decaying Modifier in a specific Province")]
        public async Task CreateLocalDecayingModifier(string ProvinceName, string ValueName, string ModifierName, string Description, string Modifier, string Decay)
        {
            ValueModifier valueModifier = new ValueModifier()
            {
                Name = ModifierName,
                Description = Description,
                Modifier = float.Parse(Modifier),
                Type = ModifierType.Temporary,
                Decay = float.Parse(Decay),
                Locality = Locality.Local
            };
            Province p = _icarusContext.Provinces.FirstOrDefault(p => p.Name == ProvinceName);
            if (p == null)
            {
                await RespondAsync("Province not Found");
            }
            Value v = p.Values.FirstOrDefault(v => v.Name == ValueName);
            if (v == null)
            {
                await RespondAsync("Value not Found");
            }
            v.Modifiers.Add(valueModifier);
            _icarusContext.Modifiers.Add(valueModifier);
            await _icarusContext.SaveChangesAsync();
            await RespondAsync($"Created new decaying local Modifier at {p.Name} affecting {v.Name} by {valueModifier.Modifier} decaying by {valueModifier.Decay} each Tick");
        }
        */
    }
}
