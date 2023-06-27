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
using Icarus.Discord.CustomPreconditions;
using System.IO;
using Icarus.Utils;
using System.Net.Http;

namespace Icarus.Discord.EconCommands
{
    public class ValuesModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DiscordSocketClient _client;
        private readonly ValueManagementService _valueManagementService;
        private readonly DebugService _debugService;

        public ValuesModule(DiscordSocketClient client, ValueManagementService valueManagementService, DebugService debugService)
        {
            _client = client;
            _valueManagementService = valueManagementService;
            _debugService = debugService;
        }

        [SlashCommand("setvalue","Set the specified Value to a certain Number.")]
        [RequireAdmin]
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

        [SlashCommand("showvalue", "Show specific info about a Value")]
        public async Task ShowValue(string ProvinceName, string ValueTAG)
        {
            using var db = new IcarusContext();

            Value value = db.Provinces.FirstOrDefault(p => p.Name == ProvinceName).Values.FirstOrDefault(v => v.TAG == ValueTAG);
            if (value == null)
            {
                await RespondAsync($"{ValueTAG} was not found in {ProvinceName}");
                return;
            }

            await DeferAsync();

            EmbedBuilder emb = new EmbedBuilder()
            {
                Title = $"{value.Name} in {ProvinceName}",
                Description = value.Description,
            };

            float valueGoal = _valueManagementService.GetValueGoal(value);

            EmbedFieldBuilder CurrentValue = new EmbedFieldBuilder()
                .WithName("Current Value")
                .WithValue(value.CurrentValue);

            EmbedFieldBuilder Goal = new EmbedFieldBuilder()
                .WithName("Goal")
                .WithValue(valueGoal.ToString());

            EmbedFieldBuilder Change = new EmbedFieldBuilder()
                .WithName("Change")
                .WithValue(_valueManagementService.GetValueChange(value).ToString());

            List<float> heights = new List<float>();

            foreach(ValueHistory hist in value.PastValues)
            {
                heights.Add(hist.Height);
            }

            await GenChart(heights, valueGoal);

            try
            {
                //emb.WithImageUrl(@"attachment://D:\SeasonDPS\Icarus\Icarus\Images\.chart.png");
                
                emb.AddField(CurrentValue);
                emb.AddField(Goal);
                emb.AddField(Change);
                //await Context.Channel.SendMessageAsync(embed:emb.Build());

                await FollowupWithFileAsync(@"\publish\Images\.chart.png", embed: emb.Build());
                //await FollowupWithFileAsync(@"D:\SeasonDPS\Icarus\Icarus\Images\.chart.png", embed: emb.Build());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            
        }

        public async Task GenChart(List<float> values, float goal)
        {
            var icarusConfig = ConfigFactory.GetConfig();

            try
            {
                using HttpClient client = new();
                var m = await client.GetAsync($"http://127.0.0.1:5000/genChart/{string.Join(",", values)}/{goal}/");
                string t = await m.Content.ReadAsStringAsync();
                _ = _debugService.PrintToChannels(t);
                Console.WriteLine(t);


            }
            catch (Exception ex)
            {
                _ = _debugService.PrintToChannels(ex.Message);
                Console.WriteLine(ex.ToString());
            }

            /*const string cmd = "bash";
            string args = $"";
            const string activateVenv = "source .python/bin/activate";
            var commandsToExecute = new List<string>(){
                //"pip install -r requirements.txt",
                $"python ChartGen.py {string.Join(",", values)} {goal}"
            };

            string test = $"{string.Join(",", values)}";

            var startInfo = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = args,
                FileName = cmd,
                WorkingDirectory = icarusConfig.PythonScriptLocation
            };

            try
            {
                var process = Process.Start(startInfo);
                if (process == null)
                    throw new Exception("Could not start process");

                using var sw = process.StandardInput;
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine(activateVenv);
                    foreach (var command in commandsToExecute)
                    {
                        sw.WriteLine(command);
                    }
                    sw.Flush();
                    sw.Close();
                }

                var sb = new StringBuilder();
                while (!process.HasExited)
                    sb.Append(process.StandardOutput.ReadToEnd());

                var error = process.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(error))
                    throw new Exception($"Something went wrong: \n{error}");
                Console.WriteLine(sb.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                _ = _debugService.PrintToChannels(ex.Message);
            }*/
        }
    }
}
