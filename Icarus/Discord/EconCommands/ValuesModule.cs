using Discord;
//using Discord.Commands;
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
using SixLabors.ImageSharp;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions.Generated;
using Icarus.Migrations;

namespace Icarus.Discord.EconCommands
{
    [Group("value", "Value Commands")]
    public class ValuesModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DiscordSocketClient _client;
        private readonly ValueManagementService _valueManagementService;
        private readonly DebugService _debugService;
        private readonly EconVisualsService _visualsService;
        private readonly DiscordInteractionHelpers _interactionHelpers;

        public ValuesModule(DiscordSocketClient client, ValueManagementService valueManagementService, DebugService debugService, EconVisualsService visualsService, DiscordInteractionHelpers interactionHelpers)
        {
            _client = client;
            _valueManagementService = valueManagementService;
            _debugService = debugService;
            _visualsService = visualsService;
            _interactionHelpers = interactionHelpers;
        }

        [SlashCommand("set","Set the specified Value to a certain Number.")]
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

        [SlashCommand("show-many", "Show Values of a Province")]
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

        [SlashCommand("show-province","Show general Province Info")]
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

        [SlashCommand("show", "Show specific info about a Value")]
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
            Dictionary<string,float> valueGoalDesc = _valueManagementService.GetValueGoalDesc(value);

            EmbedFieldBuilder CurrentValue = new EmbedFieldBuilder()
                .WithName("Current Value")
                .WithValue(value.CurrentValue);

            EmbedFieldBuilder Goal = new EmbedFieldBuilder()
                .WithName("Goal")
                .WithValue(valueGoal.ToString());

            EmbedFieldBuilder Change = new EmbedFieldBuilder()
                .WithName("Change")
                .WithValue(_valueManagementService.GetValueChange(value).ToString());

            EmbedFieldBuilder MakeUp = new EmbedFieldBuilder()
                .WithName("MakeUp");
            StringBuilder stringBuilder= new StringBuilder();
            foreach(KeyValuePair<string,float> kvp in valueGoalDesc)
            {
                stringBuilder.AppendLine($"{kvp.Key} : {kvp.Value}");
            }
            MakeUp.Value = stringBuilder.ToString();

            List<float> heights = new List<float>();

            foreach(ValueHistory hist in value.PastValues)
            {
                heights.Add(hist.Height);
            }

            MemoryStream m = await GenChart(heights, valueGoal);
            if (m == null) 
            {
                await FollowupAsync("Could not retrieve Chart!");
            }

            try
            {
                //emb.WithImageUrl(@"attachment://D:\SeasonDPS\Icarus\Icarus\Images\.chart.png");
                
                emb.AddField(CurrentValue);
                emb.AddField(Goal);
                emb.AddField(Change);
                emb.AddField(MakeUp);
                //await Context.Channel.SendMessageAsync(embed:emb.Build());

                //await FollowupWithFileAsync(@"\publish\Images\.chart.png", embed: emb.Build());
                FileAttachment f = new FileAttachment(m, "Chart.png");
                await FollowupWithFileAsync(f, embed: emb.Build());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                await FollowupAsync(ex.Message);
            }
            
        }

        public async Task<MemoryStream> GenChart(List<float> values, float goal)
        {
            var icarusConfig = ConfigFactory.GetConfig();

            try
            {
                using HttpClient client = new();
                var m = await client.GetAsync($"http://localhost:5000/genChart/{string.Join(",", values)}/{goal}/");
                string t = await m.Content.ReadAsStringAsync();
                ChartDTO? chart = JsonSerializer.Deserialize<ChartDTO>(t);
                byte[] bytes = Convert.FromBase64String(chart.Base64String);

                SixLabors.ImageSharp.Image image;

                return new MemoryStream(bytes);

                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    image = SixLabors.ImageSharp.Image.Load(ms);
                }
                string path = @".\Image\Chart.png";
                image.SaveAsPng(path);
                _ = _debugService.PrintToChannels(t);
                Console.WriteLine(t);


            }
            catch (Exception ex)
            {
                _ = _debugService.PrintToChannels(ex.Message);
                Console.WriteLine(ex.ToString());
                return null;
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

        [SlashCommand("update-sheet","update sheet")]
        [RequireAdmin]
        public async Task UpdateSheet()
        {
            await DeferAsync();
            try
            {
                
                await _visualsService.UpdateProvinceView(_valueManagementService);
                await FollowupAsync("Success!", ephemeral:true);
            }
            catch(Exception ex)
            {
                IUserMessage m;
                if (ex.GetType() == typeof(Google.GoogleApiException)) 
                {
                   m = await FollowupAsync("There has been an issue with the GoogleAPI. We have propably reached our write limit. Please wait a few minutes and try again.", ephemeral:true);
                }
                else
                {
                   m = await FollowupAsync("Something went wrong. Please contact an admin.", ephemeral:true);
                }
                _ = _debugService.PrintToChannels($"{ex.ToString()}");
            }
        }

        [SlashCommand("edit-relationship", "Edit a Value Relationship")]
        public async Task EditRel(string Origin, string Target)
        {
            using var db = new IcarusContext();
            string messageId = Random.Shared.Next(9999).ToString();
            var rel = db.Relationships.FirstOrDefault(rl => rl.OriginTag== Origin && rl.TargetTag == Target);
            if (rel == null)
            {
                await RespondAsync("Relationship not found");
                return;
            }

            ModalBuilder mb = new ModalBuilder()
                .WithTitle("Edit Relationship")
                .WithCustomId(_interactionHelpers.GenerateCompoundId(messageId, "editrel"))
                .AddTextInput("Origin", "Origin", value: rel.OriginTag)
                .AddTextInput("Target", "Target", value: rel.TargetTag)
                .AddTextInput("Weight", "Weight", value: rel.Weight.ToString());

            await RespondWithModalAsync(mb.Build());

            Predicate<SocketInteraction> modalPred = s =>
            {
                if (s.GetType() != typeof(SocketModal)) return false;
                SocketModal d = (SocketModal)s;
                return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId, "editrel");
            };

            var responseModal = (SocketModal)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 10, 0), modalPred);

            if (responseModal == null)
            {
                return;
            }

            rel.Weight = float.Parse(responseModal.Data.Components.FirstOrDefault(c => c.CustomId == "Weight").Value, System.Globalization.CultureInfo.InvariantCulture);
            rel.TargetTag = responseModal.Data.Components.FirstOrDefault(c => c.CustomId == "Target").Value;
            rel.OriginTag = responseModal.Data.Components.FirstOrDefault(c => c.CustomId == "Origin").Value;

            await db.SaveChangesAsync();
            await responseModal.RespondAsync("Success", ephemeral: true);
        }

        [SlashCommand("add-relationship", "Add a new Relationship")]
        public async Task AddRel()
        {
            using var db = new IcarusContext();
            string messageId = Random.Shared.Next(9999).ToString();
            ModalBuilder mb = new ModalBuilder()
                .WithTitle("Edit Relationship")
                .WithCustomId(_interactionHelpers.GenerateCompoundId(messageId, "editrel"))
                .AddTextInput("Origin", "Origin" )
                .AddTextInput("Target", "Target" )
                .AddTextInput("Weight", "Weight", value: "1");

            await RespondWithModalAsync(mb.Build());

            Predicate<SocketInteraction> modalPred = s =>
            {
                if (s.GetType() != typeof(SocketModal)) return false;
                SocketModal d = (SocketModal)s;
                return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId, "editrel");
            };

            var responseModal = (SocketModal)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 10, 0), modalPred);

            if (responseModal == null)
            {
                return;
            }

            string TargetTag = responseModal.Data.Components.FirstOrDefault(c => c.CustomId == "Target").Value;
            string OriginTag = responseModal.Data.Components.FirstOrDefault(c => c.CustomId == "Origin").Value;

            if (db.Relationships.FirstOrDefault(rel => rel.TargetTag == TargetTag && rel.OriginTag == OriginTag) != null)
            {
                await responseModal.RespondAsync("Relationship already exists");
                return;
            }
            if (db.Values.FirstOrDefault(v => v.TAG == TargetTag || v.TAG == OriginTag)== null)
            {
                await responseModal.RespondAsync("Not a valid Tag");
                return;
            }

            var rel = new ValueRelationship()
            {
                OriginTag = OriginTag,
                TargetTag = TargetTag,
                Weight = float.Parse(responseModal.Data.Components.FirstOrDefault(c => c.CustomId == "Weight").Value, System.Globalization.CultureInfo.InvariantCulture)
            };

            db.Relationships.Add(rel);
            await db.SaveChangesAsync();
            await responseModal.RespondAsync("Success", ephemeral: true);
        }

        [SlashCommand("remove-relationship", "Remove a Value Relationship")]
        public async Task RemoveRel(string Origin, string Target)
        {
            using var db = new IcarusContext();
            string messageId = Random.Shared.Next(9999).ToString();
            var rel = db.Relationships.FirstOrDefault(rl => rl.OriginTag == Origin && rl.TargetTag == Target);
            if (rel == null)
            {
                await RespondAsync("Relationship not found");
                return;
            }

            db.Relationships.Remove(rel);

            await db.SaveChangesAsync();
            await RespondAsync("Success", ephemeral: true);
        }

        [SlashCommand("show-relationships", "Show Relationships")]
        public async Task ShowRel()
        {
            using var db = new IcarusContext();
            List<ValueRelationship> rels = db.Relationships.ToList();
            EmbedBuilder emb = new EmbedBuilder()
            {
                Title = $"Relationsships",
                Description = "List of all Relationships and their Weights",
            };

            EmbedFieldBuilder f = new EmbedFieldBuilder();
            StringBuilder stringBuilder= new StringBuilder();
            foreach(var r in rels)
            {
                stringBuilder.AppendLine($"{r.OriginTag}->{r.TargetTag} : {r.Weight}");
            }
            f.WithName("List");
            f.WithValue(stringBuilder.ToString());
            emb.AddField(f);
            await RespondAsync(embed: emb.Build());
        }

    }

    public class ChartDTO
    {
        public string Base64String { get; set; }
    }
}
