using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
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

namespace Icarus.Discord.EconCommands
{
    public class ModifierCreation : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DiscordSocketClient _client;
        private readonly ValueManagementService _valueManagementService;

        public ModifierCreation(DiscordSocketClient client, ValueManagementService valueManagementService)
        {
            _client = client;
            _valueManagementService = valueManagementService;

            _client.ModalSubmitted += ModifierHeightHandling;
        }

        //Creation of Modifier Steps
        //Command to start the process
        //Type in Name and Description
        //Choose Provinces or Global
        //Choose Values
        //Set Height of Modifier per Value
        //Set Type
        //Set Decay Rate or Tick Amount

        

        //Here we wanna extract the Name of the Modifier and the current step from the CustomId
        public Dictionary<string, string> ReadCompoundId(string Id)
        {
            Dictionary<string,string> result = new Dictionary<string,string>();
            List<string> data = Id.Split('_').ToList();
            result.Add("modifier", data[0]);
            if(data.Count < 2 ) 
            {
                return result;
            }
            result.Add("stage", data[1]);
            //If the stage is "amount" the compound key will also contain info about the type of value
            //if (data[1] == "Amount")
            //{
            //    result.Add("value", data[2]);
            //}

            return result;
        }

        public string GenerateCompoundId(string modifier, string stage, string value = "")
        {
            string id = modifier + "_" + stage;
            return id;
        }

        public SelectMenuBuilder CreateSelectMenu(string ModifierName, string Stage, List<string> Options, string Placeholder = "Select Option")
        {
            Debug.WriteLine("Creating Dropdown!");
            SelectMenuBuilder builder = new SelectMenuBuilder()
                .WithPlaceholder(Placeholder)
                .WithCustomId(GenerateCompoundId(ModifierName,Stage));

            foreach(string option in Options)
            {
                builder.AddOption(option, option);
            }
            Debug.WriteLine("Returning Select Menu");
            return builder;
        }

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
            [ModalTextInput(customId: "ModifierName", placeholder: "Enter Tick Amount", maxLength: 20)]
            public string Time { get; set; }
        }
        public class DecayModal : IModal
        {
            public string Title => "Decay Rate";
            // Strings with the ModalTextInput attribute will automatically become components.
            [InputLabel("Decay Rate")]
            [ModalTextInput(customId: "ModifierName", placeholder: "Enter Decay as 0.1,0.25 etc", maxLength: 20)]
            public string Decay { get; set; }
        }

        //First Command
        [SlashCommand("createmodifier", "Starts the Process of Adding a Modifier")]
        public async Task CreateModifer()
        {
            await Context.Interaction.RespondWithModalAsync<NameModal>("ModifierNameDescMenu");
        }


        //Handlers
        [ModalInteraction("ModifierNameDescMenu")]
        public async Task ModifierNameHandling(NameModal modal)
        {
            using var db = new IcarusContext();

            ModifierCreationDTO modifier = new ModifierCreationDTO()
            {
                Name = modal.Name,
                Description = modal.Description,
                Step = ModifierCreationStep.Province
            };
            _valueManagementService.Modifiers.Add(modifier);

            //On to the next stage
            List<string> provinces = new List<string>();
            provinces.Add("Global");
            foreach(Province province in db.Provinces)
            {
                provinces.Add(province.Name);
                Debug.WriteLine(province.Name);
            }
            
            SelectMenuBuilder sm = CreateSelectMenu(modifier.Name, modifier.Step.ToString(), provinces, "Select Provinces");
            sm.MinValues = 1;
            sm.MaxValues = sm.Options.Count();
            Console.WriteLine("Building Menu");
            ComponentBuilder builder = new ComponentBuilder()
                .WithSelectMenu(sm);


            // Respond to the modal.
            await RespondAsync("Choose Provinces or Global.", components:builder.Build());
        }

        [ComponentInteraction("*_Province")]
        public async Task ModifierProvinceHandling(string id, string[] selectedProvinces)
        {
            string ModifierName = id;
            ModifierCreationDTO modifier = _valueManagementService.Modifiers.FirstOrDefault(x => x.Name == ModifierName);
            modifier.Provinces = selectedProvinces.ToList();

            //Next Step
            modifier.Step = ModifierCreationStep.Values;
            List<string> ValueNames = new List<string>();
            List<Value> ValueTemplate = _valueManagementService.GenerateValueTemplate();
            foreach (Value Value in ValueTemplate)
            {
                ValueNames.Add(Value.Name);
            }
            SelectMenuBuilder sm = CreateSelectMenu(modifier.Name, modifier.Step.ToString(), ValueNames, "Select Values");
            sm.MinValues = 1;
            sm.MaxValues = sm.Options.Count();

            



            var builder = new ComponentBuilder()
                .WithSelectMenu(sm);


            Debug.WriteLine("Sending Value Dropdown");
            await DeferAsync();
            await DeleteOriginalResponseAsync();
            Debug.WriteLine("Response Sent!");
            try
            {
                await FollowupAsync("Choose Values to apply Modifier to", components: builder.Build());
            }
            catch(HttpException ex)
            {
                Debug.WriteLine(ex.Reason);
                throw ex;
            }
            
        }

        [ComponentInteraction("*_Values")]
        public async Task ModifierValuesHandling(string id, string[] selectedValues)
        {
            string ModifierName = id;
            ModifierCreationDTO modifier = _valueManagementService.Modifiers.FirstOrDefault(x => x.Name == ModifierName);

            List<string> values = selectedValues.ToList();
            foreach (string Value in values)
            {
                string h = Value.Replace("_", " ");
                modifier.ValueSizePairs.Add(h, 0.0f);
            }
            modifier.Step = ModifierCreationStep.Amount;

            TextInputBuilder tb;
            ModalBuilder mb = new ModalBuilder()
                .WithTitle("Set Height of Modifiers")
                .WithCustomId(GenerateCompoundId(modifier.Name,modifier.Step.ToString()));
            foreach (string Value in values)
            {
                tb = new TextInputBuilder()
                    .WithLabel(Value)
                    .WithCustomId(Value);
                mb.AddTextInput(tb);
            }
            //await DeleteOriginalResponseAsync();
            await Context.Interaction.RespondWithModalAsync(mb.Build());
        }

        //[ModalInteraction("*_Amount")]
        public async Task ModifierHeightHandling(SocketModal modal)
        {
            Dictionary<string,string> CompoundIdData = ReadCompoundId(modal.Data.CustomId);
            if (CompoundIdData.ContainsKey("stage") && CompoundIdData["stage"] == "Amount")
            {
                string ModifierName = CompoundIdData["modifier"];
                ModifierCreationDTO modifier = _valueManagementService.Modifiers.FirstOrDefault(m => m.Name == ModifierName);
                foreach (var Component in modal.Data.Components)
                {
                    Debug.WriteLine(Component.CustomId + ": " + Component.Value);
                    modifier.ValueSizePairs[Component.CustomId] = float.Parse(Component.Value, System.Globalization.CultureInfo.InvariantCulture);

                }
                modifier.Step = ModifierCreationStep.Type;

                var menuBuilder = new SelectMenuBuilder()
                   .WithPlaceholder("Select Type")
                   .WithCustomId(GenerateCompoundId(modifier.Name, modifier.Step.ToString()))
                   .WithMinValues(1)
                   .WithMaxValues(1)
                   .AddOption(ModifierType.Permanent.ToString(), ModifierType.Permanent.ToString())
                   .AddOption(ModifierType.Temporary.ToString(), ModifierType.Temporary.ToString())
                   .AddOption(ModifierType.Decaying.ToString(), ModifierType.Decaying.ToString());
                var builder = new ComponentBuilder()
                    .WithSelectMenu(menuBuilder);

                await modal.RespondAsync("Choose Values", components: builder.Build());
            }
        }

        

        [ComponentInteraction("*_Type")]
        public async Task ModifierTypeHandler(string id, string[] selectedType)
        {
            string ModifierName = id;
            ModifierCreationDTO modifier = _valueManagementService.Modifiers.FirstOrDefault(m => m.Name == ModifierName);
            modifier.ModifierType = Enum.Parse<ModifierType>(selectedType[0]);

            if (modifier.ModifierType == ModifierType.Permanent)
            {
                modifier.Step = ModifierCreationStep.Finalization;
                //Head on to Finalization
                await ModifierFinalization(modifier);
            }
            else
            {
                if (modifier.ModifierType == ModifierType.Temporary)
                {
                    modifier.Step = ModifierCreationStep.Temporary;
                    await Context.Interaction.RespondWithModalAsync<TemporaryModal>(GenerateCompoundId(modifier.Name,modifier.Step.ToString()));
                }
                else
                {
                    try
                    {
                        modifier.Step = ModifierCreationStep.Decay;
                        await Context.Interaction.RespondWithModalAsync<DecayModal>(GenerateCompoundId(modifier.Name, modifier.Step.ToString()));
                    }
                    catch (HttpException ex)
                    {
                        Console.WriteLine(ex.Reason);
                    }
                    
                }
            }
        }

        [ModalInteraction("*_Temporary")]
        public async Task ModifierTempHandler(string id, TemporaryModal modal)
        {
            string ModifierName = id;
            ModifierCreationDTO modifier = _valueManagementService.Modifiers.FirstOrDefault(m => m.Name == ModifierName);
            modifier.DurationOrDecay = int.Parse(modal.Time, System.Globalization.CultureInfo.InvariantCulture);
            modifier.Step = ModifierCreationStep.Finalization;
            //Finalize
            await ModifierFinalization(modifier);
        }

        [ModalInteraction("*_Decay")]
        public async Task ModifierDecayHandler(string id, DecayModal modal)
        {
            string ModifierName = id;
            ModifierCreationDTO modifier = _valueManagementService.Modifiers.FirstOrDefault(m => m.Name == ModifierName);
            modifier.DurationOrDecay = float.Parse(modal.Decay, System.Globalization.CultureInfo.InvariantCulture);
            modifier.Step = ModifierCreationStep.Finalization;
            //Finalize
            await ModifierFinalization(modifier);
        }

        public async Task ModifierFinalization(ModifierCreationDTO modifier)
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
                    Province.Modifiers.Add(CreateModifier(modifier));
                }
            }
            await db.SaveChangesAsync();
            await RespondAsync("Modifier created");
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
                ValueModifier valueModifier = new ValueModifier()
                {
                    ValueName = ValueAmountPair.Key,
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
    }

    public class ModifierCreationDTO
    {
        public string Name { get; set; }
        public string Description { get; set;}
        public float DurationOrDecay { get; set;}
        public ModifierType ModifierType { get; set;}
        public Dictionary<string, float> ValueSizePairs { get; set;} = new Dictionary<string, float>();
        public List<string> Provinces { get; set; } = new List<string>();
        public ModifierCreationStep Step { get; set;}
    }

    public enum ModifierCreationStep
    {
        Name,
        Province,
        Values,
        Amount,
        Type,
        Temporary,
        Decay,
        Finalization
    }
}
