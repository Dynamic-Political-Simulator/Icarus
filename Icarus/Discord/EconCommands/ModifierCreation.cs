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
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Icarus.Discord.EconCommands
{
    public class ModifierCreation : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IcarusContext _icarusContext;
        private readonly DiscordSocketClient _client;
        private readonly ValueManagementService _valueManagementService;
        public List<ModifierCreationDTO> Modifiers { get; set; } = new List<ModifierCreationDTO>();

        public ModifierCreation(IcarusContext icarusContext, DiscordSocketClient client, ValueManagementService valueManagementService)
        {
            _icarusContext = icarusContext;
            _client = client;
            _valueManagementService = valueManagementService;
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
            result.Add("stage", data[1]);
            //If the stage is "amount" the compound key will also contain info about the type of value
            if (data[1] == "Amount")
            {
                result.Add("value", data[2]);
            }

            return result;
        }

        public string GenerateCompoundId(string modifier, string stage, string value = "")
        {
            string id = modifier + "_" + stage;
            if (stage == "Amount")
            {
                id += "_" + value;
            }
            return id;
        }

        public SelectMenuBuilder CreateSelectMenu(string ModifierName, string Stage, List<string> Options, string Placeholder = "Select Option")
        {
            SelectMenuBuilder builder = new SelectMenuBuilder()
                .WithPlaceholder(Placeholder)
                .WithCustomId(GenerateCompoundId(ModifierName,Stage));

            foreach(string option in Options)
            {
                builder.AddOption(option, option);
            }

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
            public string Title => "Set the Time the Modifier will last!";
            // Strings with the ModalTextInput attribute will automatically become components.
            [InputLabel("Tick Amount")]
            [ModalTextInput(customId: "ModifierName", placeholder: "Enter Tick Amount", maxLength: 20)]
            public string Time { get; set; }
        }
        public class DecayModal : IModal
        {
            public string Title => "Set Decay Rate of the Modifier";
            // Strings with the ModalTextInput attribute will automatically become components.
            [InputLabel("Decay Rate (Percentage of Initial Modifier lost every Tick)")]
            [ModalTextInput(customId: "ModifierName", placeholder: "Enter Decay as e.g. 0.1,0.5,0.25 etc", maxLength: 20)]
            public string Decay { get; set; }
        }

        //First Command
        [SlashCommand("CreateModifier", "Starts the Process of Adding a Modifier")]
        public async Task CreateModifer()
        {
            await Context.Interaction.RespondWithModalAsync<NameModal>("ModifierNameDescMenu");
        }

        //Handlers
        [ModalInteraction("ModifierNameDescMenu")]
        public async Task ModifierNameHandling(NameModal modal)
        {
            ModifierCreationDTO modifier = new ModifierCreationDTO()
            {
                Name = modal.Name,
                Description = modal.Description,
                Step = ModifierCreationStep.Province
            };
            Modifiers.Add(modifier);

            //On to the next stage
            List<string> provinces = new List<string>();
            provinces.Add("Global");
            foreach(Province province in _icarusContext.Provinces)
            {
                provinces.Add(province.Name);
            }
            SelectMenuBuilder sm = CreateSelectMenu(modifier.Name, modifier.Step.ToString(), provinces, "Select Provinces");
            sm.MinValues = 1;
            sm.MaxValues = 100;

            ComponentBuilder builder = new ComponentBuilder()
                .WithSelectMenu(sm);


            // Respond to the modal.
            await RespondAsync("Choose Provinces or Global.", components:builder.Build());
        }

        [ComponentInteraction("*_Province")]
        public async Task ModifierProvinceHandling(string id, string[] selectedProvinces)
        {
            string ModifierName = ReadCompoundId(id)["modifier"];
            ModifierCreationDTO modifier = Modifiers.FirstOrDefault(x => x.Name == ModifierName);
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
            sm.MaxValues = 100;

            ComponentBuilder builder = new ComponentBuilder()
                .WithSelectMenu(sm);


            // Respond to the modal.
            await RespondAsync("Choose Values to apply Modifier to", components: builder.Build());
        }

        [ComponentInteraction("*_Values")]
        public async Task ModifierValuesHandling(string id, string[] selectedValues)
        {
            string ModifierName = ReadCompoundId(id)["modifier"];
            ModifierCreationDTO modifier = Modifiers.FirstOrDefault(x => x.Name == ModifierName);

            List<string> values = selectedValues.ToList();
            foreach (string Value in values)
            {
                modifier.ValueSizePairs.Add(Value, 0.0f);
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

            await Context.Interaction.RespondWithModalAsync(mb.Build());
        }

        [ModalInteraction("*_Amount")]
        public async Task ModifierHeightHandling(SocketModal modal)
        {
            string ModifierName = ReadCompoundId(modal.Data.CustomId)["modifier"];
            ModifierCreationDTO modifier = Modifiers.FirstOrDefault(m => m.Name == ModifierName);
            foreach(var Component in modal.Data.Components)
            {
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

            await FollowupAsync("Choose Values", components: builder.Build());
        }

        [ComponentInteraction("*_Type")]
        public async Task ModifierTypeHandler(string id, string[] selectedType)
        {
            string ModifierName = ReadCompoundId(id)["modifier"];
            ModifierCreationDTO modifier = Modifiers.FirstOrDefault(m => m.Name == ModifierName);
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
                    modifier.Step = ModifierCreationStep.Decay;
                    await Context.Interaction.RespondWithModalAsync<DecayModal>(GenerateCompoundId(modifier.Name, modifier.Step.ToString()));
                }
            }
        }

        [ModalInteraction("*_Temporary")]
        public async Task ModifierTempHandler(string id, TemporaryModal modal)
        {
            string ModifierName = ReadCompoundId(id)["modifier"];
            ModifierCreationDTO modifier = Modifiers.FirstOrDefault(m => m.Name == ModifierName);
            modifier.DurationOrDecay = int.Parse(modal.Time);
            modifier.Step = ModifierCreationStep.Finalization;
            //Finalize
            await ModifierFinalization(modifier);
        }

        [ModalInteraction("*_Decay")]
        public async Task ModifierDecayHandler(string id, DecayModal modal)
        {
            string ModifierName = ReadCompoundId(id)["modifier"];
            ModifierCreationDTO modifier = Modifiers.FirstOrDefault(m => m.Name == ModifierName);
            modifier.DurationOrDecay = int.Parse(modal.Decay);
            modifier.Step = ModifierCreationStep.Finalization;
            //Finalize
            await ModifierFinalization(modifier);
        }

        public async Task ModifierFinalization(ModifierCreationDTO modifier)
        {
            Modifier Modifier = new Modifier() 
            {
                Name = modifier.Name,
                Description = modifier.Description,
                Type = modifier.ModifierType
            };
            if (modifier.ModifierType == ModifierType.Temporary)
            {
                Modifier.Duration = (int)(modifier.DurationOrDecay);
            }

            foreach(KeyValuePair<string, float> ValueAmountPair in modifier.ValueSizePairs)
            {
                ValueModifier valueModifier = new ValueModifier() 
                {
                    ValueName = ValueAmountPair.Key,
                    Modifier = ValueAmountPair.Value,
                };
                if (modifier.ModifierType == ModifierType.Decaying)
                {
                    valueModifier.Decay = modifier.DurationOrDecay * ValueAmountPair.Value;
                }
                Modifier.Modifiers.Add(valueModifier);
            }

            if (modifier.Provinces[0] == "Global")
            {
                _icarusContext.Nations.First().Modifiers.Add(Modifier);
            }
            else
            {
                foreach(string province in modifier.Provinces)
                {
                    Province Province = _icarusContext.Provinces.FirstOrDefault(p => p.Name == province);
                    Province.Modifiers.Add(Modifier);
                }
            }
            await _icarusContext.SaveChangesAsync();
        }


    }

    public class ModifierCreationDTO
    {
        public string Name { get; set; }
        public string Description { get; set;}
        public float DurationOrDecay { get; set;}
        public ModifierType ModifierType { get; set;}
        public Dictionary<string, float> ValueSizePairs { get; set;}
        public List<string> Provinces { get; set;}
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
