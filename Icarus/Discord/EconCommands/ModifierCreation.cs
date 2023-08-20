using Azure;
using Discord;
//using Discord.Commands;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Google.Apis.Sheets.v4.Data;
using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Icarus.Discord.CustomPreconditions;

namespace Icarus.Discord.EconCommands
{
    [Group("modifier", "Modifier commands")]
    public class ModifierCreation : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DiscordSocketClient _client;
        private readonly ValueManagementService _valueManagementService;
        private readonly DiscordInteractionHelpers _interactionHelpers;
        private readonly DebugService _debugService;

        public ModifierCreation(DiscordSocketClient client, ValueManagementService valueManagementService, DiscordInteractionHelpers interactionHelpers, DebugService debugService)
        {
            _client = client;
            _valueManagementService = valueManagementService;
            _interactionHelpers = interactionHelpers;
            _debugService = debugService;
            
            
            
        }

        //Creation of Modifier Steps
        //Command to start the process
        //Type in Name and Description
        //Choose Provinces or Global
        //Choose Values
        //Set Height of Modifier per Value
        //Set Type
        //Set Decay Rate or Tick Amount


        //Modals
        public class NameModal : IModal
        {
            public string Title => "Create new Modifier";
            // Strings with the ModalTextInput attribute will automatically become components.
            [InputLabel("Name")]
            [ModalTextInput(customId:"ModifierName", placeholder: "Enter Name", maxLength: 20)]
            public string Name { get; set; }

            // Additional paremeters can be specified to further customize the input.    
            // Parameters can be optional
            [RequiredInput(true)]
            [InputLabel("Description")]
            [ModalTextInput(customId:"ModifierDescription", TextInputStyle.Paragraph, "Enter Modifier Description", maxLength: 500)]
            public string Description { get; set; }
        }
        public class TemporaryModal : IModal
        {
            public string Title => "Modifier Time";
            // Strings with the ModalTextInput attribute will automatically become components.
            [InputLabel("Tick Amount")]
            [ModalTextInput(customId: "Time", placeholder: "Enter Tick Amount", maxLength: 20)]
            public string Time { get; set; }
        }
        public class DecayModal : IModal
        {
            public string Title => "Decay Rate";
            // Strings with the ModalTextInput attribute will automatically become components.
            [InputLabel("Decay Rate")]
            [ModalTextInput(customId: "Decay", placeholder: "Enter Decay as 0.1,0.25 etc", maxLength: 20)]
            public string Decay { get; set; }
        }

        //First Command
        [SlashCommand("create", "Starts the Process of Adding a Modifier")]
        [RequireAdmin]
        public async Task CreateModifer()
        {
            string messageId = Random.Shared.Next(9999).ToString();
            ModalBuilder mb = new ModalBuilder()
                .WithTitle("Set Height of Modifiers")
                .WithCustomId(_interactionHelpers.GenerateCompoundId(messageId, "NameDescModal"))
                .AddTextInput("Name", "ModifierName", placeholder: "Enter Name")
                .AddTextInput("Description", "ModifierDescription", TextInputStyle.Paragraph);
            

            try
            {
                await Context.Interaction.RespondWithModalAsync(mb.Build());
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine(ex.Message);
                throw ex;
            }

            Predicate<SocketInteraction> NameDesc = s =>
            {
                if (s.GetType() != typeof(SocketModal)) return false;
                SocketModal d = (SocketModal)s;
                return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId, "NameDescModal");
            };

            var responseModal = (SocketModal)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 5, 0), NameDesc);

            if (responseModal == null) { return; }

            using var db = new IcarusContext();

            ModifierCreationDTO modifier = new ModifierCreationDTO()
            {
                Name = responseModal.Data.Components.FirstOrDefault(c => c.CustomId == "ModifierName").Value,
                Description = responseModal.Data.Components.FirstOrDefault(c => c.CustomId == "ModifierDescription").Value
            };

            if(db.Modifiers.FirstOrDefault(m => m.Name == modifier.Name) != null)
            {
                await responseModal.RespondAsync("Can't have multiple modifiers with the same Name.", ephemeral: true);
                return;
            }

            //On to the next stage
            List<string> provinces = new List<string>();
            provinces.Add("Global");
            foreach(Province province in db.Provinces)
            {
                provinces.Add(province.Name);
                Debug.WriteLine(province.Name);
            }
            
            SelectMenuBuilder sm = _interactionHelpers.CreateSelectMenu(messageId, "ProvinceSelection", provinces, "Select Provinces");
            sm.MinValues = 1;
            sm.MaxValues = sm.Options.Count();
            Console.WriteLine("Building Menu");
            ComponentBuilder builder = new ComponentBuilder()
                .WithSelectMenu(sm);

            await responseModal.RespondAsync("Choose Provinces or Global", components: builder.Build(), ephemeral: true);


            Predicate<SocketInteraction> ProvinceSelection = s =>
            {
                if (s.GetType() != typeof(SocketMessageComponent)) return false;
                SocketMessageComponent d = (SocketMessageComponent)s;
                return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId.ToString(), "ProvinceSelection");
            };

            SocketMessageComponent responseDrop = (SocketMessageComponent)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 1, 0), ProvinceSelection);

            if (responseDrop == null) { return; }

            modifier.Provinces = responseDrop.Data.Values.ToList();


            List<string> ValueNames = new List<string>();
            //We are just gonna grab a list of Values from the first province cause I don't wanna read the ValueTemplate in every time the command is executed.
            //Sorta nasty but should work for this purpose.
            List<Value> ValueTemplate = db.Provinces.FirstOrDefault().Values.ToList();
            foreach (Value Value in ValueTemplate)
            {
                ValueNames.Add(Value.TAG);
            }
            sm = _interactionHelpers.CreateSelectMenu(messageId, "ValueSelection", ValueNames, "Select Values");
            sm.MinValues = 1;
            sm.MaxValues = sm.Options.Count();

            



            builder = new ComponentBuilder()
                .WithSelectMenu(sm);


            Debug.WriteLine("Sending Value Dropdown");
            try
            {
                await responseDrop.UpdateAsync(x => {
                    x.Content = "Select Values!";
                    x.Components = builder.Build();
                });
            }
            catch(HttpException ex)
            {
                Debug.WriteLine(ex.Reason);
                throw ex;
            }

            Predicate<SocketInteraction> ValueSelection = s =>
            {
                if (s.GetType() != typeof(SocketMessageComponent)) return false;
                SocketMessageComponent d = (SocketMessageComponent)s;
                return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId.ToString(), "ValueSelection");
            };

            responseDrop = (SocketMessageComponent)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 1, 0), ValueSelection);

            if (responseDrop == null) { return; }

            List<string> values = responseDrop.Data.Values.ToList();
            foreach (string Value in values)
            {
                string h = Value.Replace("_", " ");
                modifier.ValueSizePairs.Add(h, 0.0f);
            }
            values.Add("TaxModifier");
            modifier.ValueSizePairs.Add("TaxModifier", 0.0f);


            TextInputBuilder tb;
            mb = new ModalBuilder()
                .WithTitle("Set Height of Modifiers")
                .WithCustomId(_interactionHelpers.GenerateCompoundId(messageId,"ValueHeight"));
            foreach (string Value in values)
            {
                tb = new TextInputBuilder()
                    .WithLabel(Value)
                    .WithCustomId(Value);
                mb.AddTextInput(tb);
            }
            //await DeleteOriginalResponseAsync();
            //await responseDrop.UpdateAsync(x => {
            //    x.Content = "Set Value Height!";
            //    x.Components = null;
            //});
            try
            {
                await responseDrop.RespondWithModalAsync(mb.Build());
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine(ex.Message);
            }
            

            Predicate<SocketInteraction> ValueHeight = s =>
            {
                if (s.GetType() != typeof(SocketModal)) return false;
                SocketModal d = (SocketModal)s;
                return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId, "ValueHeight");
            };

            responseModal = (SocketModal)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 10, 0), ValueHeight);

            if (responseModal == null) { return; }

            foreach (var Component in responseModal.Data.Components)
            {
                Debug.WriteLine(Component.CustomId + ": " + Component.Value);
                modifier.ValueSizePairs[Component.CustomId] = float.Parse(Component.Value, System.Globalization.CultureInfo.InvariantCulture);

            }
                

            sm = new SelectMenuBuilder()
               .WithPlaceholder("Select Type")
               .WithCustomId(_interactionHelpers.GenerateCompoundId(messageId, "TypeSelection"))
               .WithMinValues(1)
               .WithMaxValues(1)
               .AddOption(ModifierType.Permanent.ToString(), ModifierType.Permanent.ToString())
               .AddOption(ModifierType.Temporary.ToString(), ModifierType.Temporary.ToString())
               .AddOption(ModifierType.Decaying.ToString(), ModifierType.Decaying.ToString());
            builder = new ComponentBuilder()
                .WithSelectMenu(sm);

            await responseModal.RespondAsync("Select Type!", components: builder.Build(), ephemeral: true);


            Predicate<SocketInteraction> TypeSelection = s =>
            {
                if (s.GetType() != typeof(SocketMessageComponent)) return false;
                SocketMessageComponent d = (SocketMessageComponent)s;
                return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId.ToString(), "TypeSelection");
            };

            responseDrop = (SocketMessageComponent)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 1, 0), TypeSelection);

            if (responseDrop == null) { return; }

            modifier.ModifierType = Enum.Parse<ModifierType>(responseDrop.Data.Values.First());

            if (modifier.ModifierType == ModifierType.Permanent)
            {
                //Head on to Finalization
                await ModifierFinalization(modifier,responseDrop);
                return;
            }
            else
            {
                mb = new ModalBuilder()
                .WithCustomId(_interactionHelpers.GenerateCompoundId(messageId, "DDConf"));
                
                if (modifier.ModifierType == ModifierType.Temporary)
                {
                    mb.WithTitle("Set Duration");
                    mb.AddTextInput(label: "Tick Amount", customId: "Time", placeholder: "Enter Tick Amount", maxLength: 20);
                    await responseDrop.RespondWithModalAsync(mb.Build());
                }
                else
                {
                    try
                    {
                        mb.WithTitle("Set Decay");
                        mb.AddTextInput(label: "Decay", customId: "Decay", placeholder: "Enter Decay as 0.1,0.25 etc", maxLength: 20);
                        await responseDrop.RespondWithModalAsync(mb.Build());
                    }
                    catch (HttpException ex)
                    {
                        Console.WriteLine(ex.Reason);
                    }
                    
                }
            }
            

            Predicate<SocketInteraction> DDConf = s =>
            {
                if (s.GetType() != typeof(SocketModal)) return false;
                SocketModal d = (SocketModal)s;
                return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId, "DDConf");
            };

            responseModal = (SocketModal)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 10, 0), DDConf);

            if (responseModal == null) { return; }

            if (modifier.ModifierType == ModifierType.Temporary)
            {
                modifier.DurationOrDecay = int.Parse(responseModal.Data.Components.FirstOrDefault(c => c.CustomId == "Time").Value, System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                modifier.DurationOrDecay = float.Parse(responseModal.Data.Components.FirstOrDefault(c => c.CustomId == "Decay").Value, System.Globalization.CultureInfo.InvariantCulture);
            }

            await ModifierFinalization(modifier, responseModal);

        }

        public async Task ModifierFinalization(ModifierCreationDTO modifier, SocketInteraction response)
        {
            using var db = new IcarusContext();

            if (modifier.Provinces[0] == "Global")
            {
                db.Nations.First().Modifiers.Add(CreateModifier(modifier));
            }
            else
            {
                foreach(string province in modifier.Provinces)
                {
                    Province Province = db.Provinces.FirstOrDefault(p => p.Name == province);
                    Modifier m = CreateModifier(modifier);
                    Province.Modifiers.Add(m);
                }
            }
            await db.SaveChangesAsync();
            await response.RespondAsync($"Modifier {modifier.Name} has been successfully created.");
        }

        public Modifier CreateModifier(ModifierCreationDTO modifierDTO)
        {
            Modifier Modifier = new Modifier()
            {
                Name = modifierDTO.Name,
                Description = modifierDTO.Description,
                Type = modifierDTO.ModifierType
            };
            if (modifierDTO.ModifierType == ModifierType.Temporary)
            {
                Modifier.Duration = (int)(modifierDTO.DurationOrDecay);
            }

            foreach (KeyValuePair<string, float> ValueAmountPair in modifierDTO.ValueSizePairs)
            {
                if(ValueAmountPair.Key == "TaxModifier")
                {
                    Modifier.WealthMod = ValueAmountPair.Value;
                    continue;
                }
                ValueModifier valueModifier = new ValueModifier()
                {
                    ValueTag = ValueAmountPair.Key,
                    Modifier = ValueAmountPair.Value,
                };
                if (modifierDTO.ModifierType == ModifierType.Decaying)
                {
                    valueModifier.Decay = modifierDTO.DurationOrDecay * ValueAmountPair.Value;
                }
                Modifier.Modifiers.Add(valueModifier);
            }
            return Modifier;
        }

        [SlashCommand("remove", "Starts the Process of removing a good from a province!")]
        [RequireAdmin]
        public async Task RemoveModifier()
        {
            using var db = new IcarusContext();

            List<string> provinces = new List<string>();
            provinces.Add("Global");
            foreach (Province province in db.Provinces.Where(p => p.Modifiers.Any(m => m.isGood == false)))
            {
                provinces.Add(province.Name);
                Debug.WriteLine(province.Name);
            }
            int messageId = Random.Shared.Next(0, 9999);
            SelectMenuBuilder sm = _interactionHelpers.CreateSelectMenu(messageId.ToString(), "ProvinceSelection", provinces, "Select Provinces");
            sm.MinValues = 1;
            sm.MaxValues = 1;
            Console.WriteLine("Building Menu");
            ComponentBuilder builder = new ComponentBuilder()
                .WithSelectMenu(sm);


            // Respond to the modal.
            await RespondAsync("Choose Province", components: builder.Build(), ephemeral: true);

            Predicate<SocketInteraction> ProvinceSelection = s =>
            {
                if (s.GetType() != typeof(SocketMessageComponent)) return false;
                SocketMessageComponent d = (SocketMessageComponent)s;
                return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId.ToString(), "ProvinceSelection");
            };

            SocketMessageComponent response = (SocketMessageComponent)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 1, 0), ProvinceSelection);

            if (response == null) { return; }
            Province _province = null;
            Nation nation = null;
            if (response.Data.Values.First() == "Global")
            {
                nation = db.Nations.FirstOrDefault();
            }
            else
            {
                _province = db.Provinces.FirstOrDefault(p => p.Name == response.Data.Values.First());
                if (_province == null)
                {
                    await RespondAsync("Province not found!");
                    return;
                }
            }

            List<string> modifiers = new List<string>();
            if(_province != null)
            {
                foreach (Modifier modifier in _province.Modifiers.Where(m => m.isGood == false))
                {
                    modifiers.Add(modifier.Name);
                    Debug.WriteLine(modifier.Name);
                    if (modifiers.Count() > 24)
                    {
                        break;
                    }
                }

                if (modifiers.Count == 0)
                {
                    await response.UpdateAsync(x => {
                        x.Content = $"{_province.Name} has no Modifiers which can be removed.";
                        x.Components = null;
                    });
                }
            }
            else
            {
                foreach (Modifier modifier in nation.Modifiers.Where(m => m.isGood == false))
                {
                    modifiers.Add(modifier.Name);
                    Debug.WriteLine(modifier.Name);
                    if (modifiers.Count() > 24)
                    {
                        break;
                    }
                }

                if (modifiers.Count == 0)
                {
                    await response.UpdateAsync(x => {
                        x.Content = $"{nation.Name} has no Modifiers which can be removed.";
                        x.Components = null;
                    });
                }
            }
            

            

            sm = _interactionHelpers.CreateSelectMenu(messageId.ToString(), "ModifierSelection", modifiers, "Select Modifier");
            sm.MinValues = 1;
            sm.MaxValues = sm.Options.Count();
            Console.WriteLine("Building Menu");
            builder = new ComponentBuilder()
                .WithSelectMenu(sm);

            await response.UpdateAsync(x => {
                x.Content = "Select Modifiers!";
                x.Components = builder.Build();
            });

            Predicate<SocketInteraction> GoodSelection = s =>
            {
                if (s.GetType() != typeof(SocketMessageComponent)) return false;
                SocketMessageComponent d = (SocketMessageComponent)s;
                return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId.ToString(), "ModifierSelection");
            };

            response = (SocketMessageComponent)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 1, 0), GoodSelection);

            if (response == null) { return; }

            foreach (string modifier in response.Data.Values)
            {
                if(nation == null)
                {
                    Modifier Modifier = _province.Modifiers.FirstOrDefault(m => m.Name == modifier);
                    if (Modifier == null)
                    {
                        await RespondAsync($"{modifier} not found");
                    }
                    db.Modifiers.Remove(Modifier);
                }
                else
                {
                    Modifier Modifier = nation.Modifiers.FirstOrDefault(m => m.Name == modifier);
                    if (Modifier == null)
                    {
                        await RespondAsync($"{modifier} not found");
                    }
                    db.Modifiers.Remove(Modifier);
                }
                
            }

            await db.SaveChangesAsync();
            await response.UpdateAsync(x => {
                x.Content = "Success!";
                x.Components = null;
            });
        }

        [SlashCommand("show", "Displays Information about a modifier")]
        public async Task ShowModifier(string province)
        {
            using var db = new IcarusContext();
            Province _province = null;
            Nation nation = null;
            int messageId = Random.Shared.Next(0, 9999);
            List<string> modifiers = new List<string>();
            if (province == null)
            {
                foreach (Modifier modifier in db.Modifiers.Where(m => m.isGood == false))
                {
                    modifiers.Add(modifier.Name);
                    Debug.WriteLine(modifier.Name);
                    if(modifiers.Count() > 24)
                    {
                        break;
                    }
                }
            }
            else
            {
                if (province == "global")
                {
                    nation = db.Nations.First();
                    foreach (Modifier modifier in nation.Modifiers.Where(m => m.isGood == false))
                    {
                        modifiers.Add(modifier.Name);
                        Debug.WriteLine(modifier.Name);

                        if (modifiers.Count() > 24)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    _province = db.Provinces.FirstOrDefault(p => p.Name == province);
                    if (_province == null)
                    {
                        await RespondAsync("Province not found!", ephemeral:true);
                        return;
                    }


                    foreach (Modifier modifier in _province.Modifiers.Where(m => m.isGood == false))
                    {
                        modifiers.Add(modifier.Name);
                        Debug.WriteLine(modifier.Name);

                        if (modifiers.Count() > 24)
                        {
                            break;
                        }
                    }
                }
            }

            if (modifiers.Count == 0)
            {
                await RespondAsync($"{province} has no Modifiers which can be displayed.",ephemeral:true);
            }

            SelectMenuBuilder sm = _interactionHelpers.CreateSelectMenu(messageId.ToString(), "ModifierSelection", modifiers, "Select Modifier");
            sm.MinValues = 1;
            sm.MaxValues = 1;
            Console.WriteLine("Building Menu");
            ComponentBuilder builder = new ComponentBuilder()
                .WithSelectMenu(sm);

            await RespondAsync("Choose Modifier",components:builder.Build(), ephemeral:true);

            Predicate<SocketInteraction> GoodSelection = s =>
            {
                if (s.GetType() != typeof(SocketMessageComponent)) return false;
                SocketMessageComponent d = (SocketMessageComponent)s;
                return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId.ToString(), "ModifierSelection");
            };

            SocketMessageComponent response = (SocketMessageComponent)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 1, 0), GoodSelection);

            if (response == null) { return; }
            Modifier Modifier;
            if (_province != null)
            {
                Modifier = _province.Modifiers.FirstOrDefault(m => m.Name == response.Data.Values.First());
            }
            else
            {
                Modifier = nation.Modifiers.FirstOrDefault(m => m.Name == response.Data.Values.First());
            }
            

            if (Modifier == null)
            {
                await response.UpdateAsync(x => {
                    x.Content = "You somehow cause an oopsie whoopsie";
                    x.Components = null;
                });
                return;
            }

            EmbedBuilder emb = new EmbedBuilder()
            {
                Title = Modifier.Name,
                Description = Modifier.Description,
            };

            StringBuilder Effects = new StringBuilder();
            foreach(ValueModifier vm in Modifier.Modifiers)
            {
                Effects.AppendLine($"{vm.ValueTag}:{vm.Modifier}");
                if (vm.Decay != 0)
                {
                    Effects.Append($" Decay: {vm.Decay}");
                }
            }
            if (Modifier.Modifiers.Count == 0)
            {
                Effects.Append("None");
            }

            emb.AddField("Effects",Effects.ToString());

            await response.UpdateAsync(x => {
                x.Content = $"Showing Modifier {Modifier.Name}";
                x.Components = null;
                x.Embed = emb.Build();
            });
        }

        [SlashCommand("edit-desc", "Edit an existing modifier")]
        [RequireAdmin]
        public async Task EditDescModifier(string province)
        {
            try
            {
                using var db = new IcarusContext();
                Province _province = null;
                Nation nation = null;
                int messageId = Random.Shared.Next(0, 9999);
                List<string> modifiers = new List<string>();

                if (province == "global")
                {
                    nation = db.Nations.First();
                    foreach (Modifier modifier in nation.Modifiers.Where(m => m.isGood == false))
                    {
                        modifiers.Add(modifier.Name);
                        Debug.WriteLine(modifier.Name);
                        if (modifiers.Count() > 24)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    _province = db.Provinces.FirstOrDefault(p => p.Name == province);
                    if (_province == null)
                    {
                        await RespondAsync("Province not found!", ephemeral: true);
                        return;
                    }


                    foreach (Modifier modifier in _province.Modifiers.Where(m => m.isGood == false))
                    {
                        modifiers.Add(modifier.Name);
                        Debug.WriteLine(modifier.Name);
                    }
                }

                if (modifiers.Count == 0)
                {
                    await RespondAsync($"{province} has no Modifiers which can be displayed.", ephemeral: true);
                }

                SelectMenuBuilder sm = _interactionHelpers.CreateSelectMenu(messageId.ToString(), "ModifierSelection", modifiers, "Select Modifier");
                sm.MinValues = 1;
                sm.MaxValues = 1;
                Console.WriteLine("Building Menu");
                ComponentBuilder builder = new ComponentBuilder()
                    .WithSelectMenu(sm);

                await RespondAsync("Choose Modifier", components: builder.Build(), ephemeral: true);

                Predicate<SocketInteraction> GoodSelection = s =>
                {
                    if (s.GetType() != typeof(SocketMessageComponent)) return false;
                    SocketMessageComponent d = (SocketMessageComponent)s;
                    return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId.ToString(), "ModifierSelection");
                };

                SocketMessageComponent response = (SocketMessageComponent)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 1, 0), GoodSelection);

                if (response == null) { return; }

                Modifier Modifier;
                if (_province != null)
                {
                    Modifier = _province.Modifiers.FirstOrDefault(m => m.Name == response.Data.Values.First());
                }
                else
                {
                    Modifier = nation.Modifiers.FirstOrDefault(m => m.Name == response.Data.Values.First());
                }

                ModalBuilder mb = new ModalBuilder()
                    .WithTitle("Name and Description of Modifiers")
                    .WithCustomId(_interactionHelpers.GenerateCompoundId(messageId.ToString(), "NameDescModal"))
                    .AddTextInput("Name", "ModifierName", placeholder: "Enter Name", value: Modifier.Name)
                    .AddTextInput("Description", "ModifierDescription", TextInputStyle.Paragraph, value: Modifier.Description);


                try
                {
                    await response.RespondWithModalAsync(mb.Build());
                }
                catch (ArgumentException ex)
                {
                    Debug.WriteLine(ex.Message);
                    throw ex;
                }

                Predicate<SocketInteraction> NameDesc = s =>
                {
                    if (s.GetType() != typeof(SocketModal)) return false;
                    SocketModal d = (SocketModal)s;
                    return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId.ToString(), "NameDescModal");
                };

                var responseModal = (SocketModal)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 5, 0), NameDesc);

                if (responseModal == null) { return; }

                

                Modifier.Name = responseModal.Data.Components.FirstOrDefault(c => c.CustomId == "ModifierName").Value;
                Modifier.Description = responseModal.Data.Components.FirstOrDefault(c => c.CustomId == "ModifierDescription").Value;

                await db.SaveChangesAsync();
                await responseModal.RespondAsync("Success!", ephemeral: true);
            }
            catch (Exception ex)
            {
                _ = _debugService.PrintToChannels(ex.ToString());
            }
        }

        [SlashCommand("edit-values", "Edit an existing modifier")]
        [RequireAdmin]
        public async Task EditValuesModifier(string province)
        {
            try
            {
                using var db = new IcarusContext();
                Province _province = null;
                Nation nation = null;
                int messageId = Random.Shared.Next(0, 9999);
                List<string> modifiers = new List<string>();

                if (province == "global")
                {
                    nation = db.Nations.First();
                    foreach (Modifier modifier in nation.Modifiers.Where(m => m.isGood == false))
                    {
                        modifiers.Add(modifier.Name);
                        Debug.WriteLine(modifier.Name);
                        if (modifiers.Count() > 24)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    _province = db.Provinces.FirstOrDefault(p => p.Name == province);
                    if (_province == null)
                    {
                        await RespondAsync("Province not found!", ephemeral: true);
                        return;
                    }


                    foreach (Modifier modifier in _province.Modifiers.Where(m => m.isGood == false))
                    {
                        modifiers.Add(modifier.Name);
                        Debug.WriteLine(modifier.Name);
                    }
                }

                if (modifiers.Count == 0)
                {
                    await RespondAsync($"{province} has no Modifiers which can be displayed.", ephemeral: true);
                }

                SelectMenuBuilder sm = _interactionHelpers.CreateSelectMenu(messageId.ToString(), "ModifierSelection", modifiers, "Select Modifier");
                sm.MinValues = 1;
                sm.MaxValues = 1;
                Console.WriteLine("Building Menu");
                ComponentBuilder builder = new ComponentBuilder()
                    .WithSelectMenu(sm);

                await RespondAsync("Choose Modifier", components: builder.Build(), ephemeral: true);

                Predicate<SocketInteraction> GoodSelection = s =>
                {
                    if (s.GetType() != typeof(SocketMessageComponent)) return false;
                    SocketMessageComponent d = (SocketMessageComponent)s;
                    return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId.ToString(), "ModifierSelection");
                };

                SocketMessageComponent response = (SocketMessageComponent)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 1, 0), GoodSelection);

                if (response == null) { return; }

                Modifier Modifier;
                if (_province != null)
                {
                    Modifier = _province.Modifiers.FirstOrDefault(m => m.Name == response.Data.Values.First());
                }
                else
                {
                    Modifier = nation.Modifiers.FirstOrDefault(m => m.Name == response.Data.Values.First());
                }

                Dictionary<string, float> values = new Dictionary<string, float>();
                foreach (ValueModifier vm in Modifier.Modifiers)
                {
                    values.Add(vm.ValueTag, vm.Modifier);
                }
                values.Add("TaxModifier", Modifier.WealthMod);

                TextInputBuilder tb;
                ModalBuilder mb = new ModalBuilder()
                    .WithTitle("Set Height of Modifiers")
                    .WithCustomId(_interactionHelpers.GenerateCompoundId(messageId.ToString(), "ValueHeight"));
                foreach (KeyValuePair<string, float> kvp in values)
                {
                    tb = new TextInputBuilder()
                        .WithLabel(kvp.Key)
                        .WithCustomId(kvp.Key)
                        .WithValue(kvp.Value.ToString());
                    mb.AddTextInput(tb);
                }
                try
                {

                    await response.RespondWithModalAsync(mb.Build());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine(ex.Message);
                }


                Predicate<SocketInteraction> ValueHeight = s =>
                {
                    if (s.GetType() != typeof(SocketModal)) return false;
                    SocketModal d = (SocketModal)s;
                    return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId.ToString(), "ValueHeight");
                };

                var responseModal = (SocketModal)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 10, 0), ValueHeight);

                if (responseModal == null) { return; }



                foreach (var Component in responseModal.Data.Components)
                {
                    if (Component.CustomId == "TaxModifier")
                    {
                        Modifier.WealthMod = float.Parse(Component.Value, System.Globalization.CultureInfo.InvariantCulture);
                        continue;
                    }
                    Debug.WriteLine(Component.CustomId + ": " + Component.Value);
                    ValueModifier vm = Modifier.Modifiers.FirstOrDefault(vm => vm.ValueTag == Component.CustomId);
                    vm.Modifier = float.Parse(Component.Value, System.Globalization.CultureInfo.InvariantCulture);

                }

                await db.SaveChangesAsync();
                await responseModal.RespondAsync("Success!", ephemeral: true);
            }
            catch (Exception ex)
            {
                _ = _debugService.PrintToChannels(ex.ToString());
            }
        }

        [SlashCommand("edit-decay", "Edit decay of an existing modifier")]
        [RequireAdmin]
        public async Task EditDecayModifier(string province)
        {
            try
            {
                using var db = new IcarusContext();
                Province _province = null;
                Nation nation = null;
                int messageId = Random.Shared.Next(0, 9999);
                List<string> modifiers = new List<string>();

                if (province == "global")
                {
                    nation = db.Nations.First();
                    foreach (Modifier modifier in nation.Modifiers.Where(m => m.isGood == false))
                    {
                        modifiers.Add(modifier.Name);
                        Debug.WriteLine(modifier.Name);
                        if (modifiers.Count() > 24)
                        {
                            break;
                        }
                    }

                }
                else
                {
                    _province = db.Provinces.FirstOrDefault(p => p.Name == province);
                    if (_province == null)
                    {
                        await RespondAsync("Province not found!", ephemeral: true);
                        return;
                    }


                    foreach (Modifier modifier in _province.Modifiers.Where(m => m.isGood == false && m.Type == ModifierType.Decaying))
                    {
                        modifiers.Add(modifier.Name);
                        Debug.WriteLine(modifier.Name);
                    }
                }

                if (modifiers.Count == 0)
                {
                    await RespondAsync($"{province} has no Modifiers which can be displayed.", ephemeral: true);
                }

                SelectMenuBuilder sm = _interactionHelpers.CreateSelectMenu(messageId.ToString(), "ModifierSelection", modifiers, "Select Modifier");
                sm.MinValues = 1;
                sm.MaxValues = 1;
                Console.WriteLine("Building Menu");
                ComponentBuilder builder = new ComponentBuilder()
                    .WithSelectMenu(sm);

                await RespondAsync("Choose Modifier", components: builder.Build(), ephemeral: true);

                Predicate<SocketInteraction> GoodSelection = s =>
                {
                    if (s.GetType() != typeof(SocketMessageComponent)) return false;
                    SocketMessageComponent d = (SocketMessageComponent)s;
                    return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId.ToString(), "ModifierSelection");
                };

                SocketMessageComponent response = (SocketMessageComponent)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 1, 0), GoodSelection);

                if (response == null) { return; }

                Modifier Modifier;
                if (_province != null)
                {
                    Modifier = _province.Modifiers.FirstOrDefault(m => m.Name == response.Data.Values.First());
                }
                else
                {
                    Modifier = nation.Modifiers.FirstOrDefault(m => m.Name == response.Data.Values.First());
                }

                Dictionary<string, float> values = new Dictionary<string, float>();
                foreach (ValueModifier vm in Modifier.Modifiers)
                {
                    values.Add(vm.ValueTag, vm.Decay);
                }

                TextInputBuilder tb;
                ModalBuilder mb = new ModalBuilder()
                    .WithTitle("Set Decay of Modifiers")
                    .WithCustomId(_interactionHelpers.GenerateCompoundId(messageId.ToString(), "ValueHeight"));
                foreach (KeyValuePair<string, float> kvp in values)
                {
                    tb = new TextInputBuilder()
                        .WithLabel(kvp.Key)
                        .WithCustomId(kvp.Key)
                        .WithValue(kvp.Value.ToString());
                    mb.AddTextInput(tb);
                }
                try
                {

                    await response.RespondWithModalAsync(mb.Build());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine(ex.Message);
                }


                Predicate<SocketInteraction> ValueHeight = s =>
                {
                    if (s.GetType() != typeof(SocketModal)) return false;
                    SocketModal d = (SocketModal)s;
                    return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId.ToString(), "ValueHeight");
                };

                var responseModal = (SocketModal)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 10, 0), ValueHeight);

                if (responseModal == null) { return; }



                foreach (var Component in responseModal.Data.Components)
                {
                    Debug.WriteLine(Component.CustomId + ": " + Component.Value);
                    ValueModifier vm = Modifier.Modifiers.FirstOrDefault(vm => vm.ValueTag == Component.CustomId);
                    vm.Decay = (float)Math.Round(float.Parse(Component.Value, System.Globalization.CultureInfo.InvariantCulture),2);

                }

                await db.SaveChangesAsync();
                await responseModal.RespondAsync("Success!", ephemeral: true);
            }
            catch (Exception ex)
            {
                _ = _debugService.PrintToChannels(ex.ToString());
            }
        }

        [SlashCommand("update-time","Update Time of temporary modifier")]
        [RequireAdmin]
        public async Task UpdateTime(string province)
        {
            using var db = new IcarusContext();
            Province _province = null;
            Nation nation = null;
            int messageId = Random.Shared.Next(0, 9999);
            List<string> modifiers = new List<string>();

            if (province == "global")
            {
                nation = db.Nations.First();
                foreach (Modifier modifier in nation.Modifiers.Where(m => m.isGood == false))
                {
                    modifiers.Add(modifier.Name);
                    Debug.WriteLine(modifier.Name);
                }
            }
            else
            {
                _province = db.Provinces.FirstOrDefault(p => p.Name == province);
                if (_province == null)
                {
                    await RespondAsync("Province not found!", ephemeral: true);
                    return;
                }


                foreach (Modifier modifier in _province.Modifiers.Where(m => m.isGood == false && m.Type == ModifierType.Temporary))
                {
                    modifiers.Add(modifier.Name);
                    Debug.WriteLine(modifier.Name);
                }
            }

            if (modifiers.Count == 0)
            {
                await RespondAsync($"{province} has no temporary Modifiers which can be displayed.", ephemeral: true);
            }

            SelectMenuBuilder sm = _interactionHelpers.CreateSelectMenu(messageId.ToString(), "ModifierSelection", modifiers, "Select Modifier");
            sm.MinValues = 1;
            sm.MaxValues = 1;
            Console.WriteLine("Building Menu");
            ComponentBuilder builder = new ComponentBuilder()
                .WithSelectMenu(sm);

            await RespondAsync("Choose Modifier", components: builder.Build(), ephemeral: true);

            Predicate<SocketInteraction> GoodSelection = s =>
            {
                if (s.GetType() != typeof(SocketMessageComponent)) return false;
                SocketMessageComponent d = (SocketMessageComponent)s;
                return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId.ToString(), "ModifierSelection");
            };

            SocketMessageComponent response = (SocketMessageComponent)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 1, 0), GoodSelection);

            if (response == null) { return; }

            Modifier Modifier;
            if (_province != null)
            {
                Modifier = _province.Modifiers.FirstOrDefault(m => m.Name == response.Data.Values.First());
            }
            else
            {
                Modifier = nation.Modifiers.FirstOrDefault(m => m.Name == response.Data.Values.First());
            }

            if (Modifier.Type != ModifierType.Temporary) 
            {
                await RespondAsync("Not a temporary Modifier");
                return;
            }

            ModalBuilder mb = new ModalBuilder()
                .WithTitle("Set Height of Modifiers")
                .WithCustomId(_interactionHelpers.GenerateCompoundId(messageId.ToString(), "TimeModal"))
                .AddTextInput(label: "Tick Amount", customId: "Time", placeholder: "Enter Tick Amount", maxLength: 20, value:Modifier.Duration.ToString());


            try
            {
                await response.RespondWithModalAsync(mb.Build());
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine(ex.Message);
                throw ex;
            }

            Predicate<SocketInteraction> NameDesc = s =>
            {
                if (s.GetType() != typeof(SocketModal)) return false;
                SocketModal d = (SocketModal)s;
                return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId.ToString(), "TimeModal");
            };

            var responseModal = (SocketModal)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 5, 0), NameDesc);

            if (responseModal == null) { return; }


            Modifier.Duration = int.Parse(responseModal.Data.Components.FirstOrDefault(c => c.CustomId == "Time").Value, System.Globalization.CultureInfo.InvariantCulture);

            await db.SaveChangesAsync();
            await responseModal.RespondAsync("Success!", ephemeral: true);
        }

    }

    public class ModifierCreationDTO
    {
        public string Name { get; set; }
        public string Description { get; set;}
        public float DurationOrDecay { get; set;}
        public ModifierType ModifierType { get; set;}
        public Dictionary<string, float> ValueSizePairs { get; set;} = new Dictionary<string, float>();
        public List<string> Provinces { get; set; } = new List<string>();
    }
}
