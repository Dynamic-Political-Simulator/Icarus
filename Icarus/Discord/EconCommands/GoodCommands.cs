﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Discord.API;
using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Icarus.Discord.EconCommands.ModifierCreation;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Icarus.Discord.CustomPreconditions;

namespace Icarus.Discord.EconCommands
{
    [Group("good", "Commands for manage Goods!")]
    public class GoodCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DiscordSocketClient _client;
        private readonly ValueManagementService _valueManagementService;
        private readonly DiscordInteractionHelpers _interactionHelpers;
        private readonly InteractionService _interactionService;
        private readonly DebugService _debugService;

        public GoodCommands(DiscordSocketClient client, ValueManagementService valueManagementService, DiscordInteractionHelpers interactionHelpers, DebugService debugService)
        {
            _client = client;
            _valueManagementService = valueManagementService;
            _interactionHelpers = interactionHelpers;
            _interactionService = new InteractionService(_client.Rest);
            _debugService = debugService;

            //_client.ModalSubmitted += ModifierHeightHandling;
        }

        [SlashCommand("add", "Starts the Process to add a good to a province!")]
        [RequireAdmin]
        public async Task CreateGood()
        {
            using var db = new IcarusContext();

            List<string> provinces = new List<string>();
            foreach (Province province in db.Provinces)
            {
                provinces.Add(province.Name);
                Debug.WriteLine(province.Name);
            }
            int messageId = Random.Shared.Next(0, 9999);

            SelectMenuBuilder sm = _interactionHelpers.CreateSelectMenu(messageId.ToString(), "ProvinceSelection", provinces, "Select Provinces");
            sm.MinValues = 1;
            sm.MaxValues = sm.Options.Count();
            Console.WriteLine("Building Menu");
            ComponentBuilder builder = new ComponentBuilder()
                .WithSelectMenu(sm);


            await RespondAsync("Choose Provinces", components: builder.Build(), ephemeral: true);

            Predicate<SocketInteraction> ProvinceSelection = s =>
            {
                if (s.GetType() != typeof(SocketMessageComponent)) return false;
                SocketMessageComponent d = (SocketMessageComponent)s;
                return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId.ToString(), "ProvinceSelection");
            };

            SocketMessageComponent response = (SocketMessageComponent)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 1, 0), ProvinceSelection);

            provinces = response.Data.Values.ToList();

            List<string> goods = new List<string>();
            foreach (Good good in db.Goods)
            {
                goods.Add(good.Name);
                Debug.WriteLine(good.Name);
            }

            
            sm = _interactionHelpers.CreateSelectMenu(messageId.ToString(), "GoodSelection", goods, "Select Goods");
            sm.MinValues = 1;
            sm.MaxValues = sm.Options.Count();
            Console.WriteLine("Building Menu");
            builder = new ComponentBuilder()
                .WithSelectMenu(sm);

            await response.UpdateAsync(x => {
                x.Content = "Select Goods!";
                x.Components = builder.Build();
            });


            Predicate<SocketInteraction> GoodSelection = s =>
            {
                if (s.GetType() != typeof(SocketMessageComponent)) return false;
                SocketMessageComponent d = (SocketMessageComponent)s;
                return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId.ToString(), "GoodSelection");
            };

            response = (SocketMessageComponent)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 1, 0), GoodSelection);

            Console.WriteLine(response.ToString());

            if (response == null) 
            {
                _ = _debugService.PrintToChannels("Response was null");
                return;
            }
            try
            {
                await response.DeferAsync(ephemeral: true);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            

            StringBuilder answer = new StringBuilder();

            foreach (string prID in provinces)
            {
                Province province = db.Provinces.FirstOrDefault(p => p.Name.Equals(prID));
                if (province != null)
                {
                    try
                    {
                        foreach (string good in response.Data.Values)
                        {
                            Good GoodTemplate = db.Goods.FirstOrDefault(g => g.Name == good);
                            if (GoodTemplate == null)
                            {
                                await response.FollowupAsync($"{good} could not be found", ephemeral:true);
                            }
                            else
                            {
                                if (province.Modifiers.Any(m => m.Name.Equals(good)))
                                {
                                    answer.AppendLine($"Good {good} was not added as province {province.Name} alread has the good {good}");
                                    continue;
                                }
                                answer.AppendLine($"Good {good} was added to province {province.Name}");
                                _valueManagementService.AddGoodToProvince(GoodTemplate, province);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        await response.FollowupAsync(ex.ToString() );
                    }
                    
                }
                else
                {
                    await response.FollowupAsync($"{prID} could not be found", ephemeral: true);
                }
            }
            await response.FollowupAsync(answer.ToString(), ephemeral: true);
            await db.SaveChangesAsync();
            

        }


        [SlashCommand("remove", "Starts the Process of removing a good from a province!")]
        [RequireAdmin]
        public async Task RemoveGood()
        {
            using var db = new IcarusContext();

            List<string> provinces = new List<string>();
            foreach (Province province in db.Provinces.Where(p => p.Modifiers.Any(m => m.isGood == true)))
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

            Province _province = db.Provinces.FirstOrDefault(p => p.Name == response.Data.Values.First());
            if (_province == null)
            {
                await RespondAsync("Province not found!");
                return;
            }

            List<string> goods = new List<string>();
            foreach (Modifier good in _province.Modifiers.Where(m => m.isGood == true))
            {
                goods.Add(good.Name);
                Debug.WriteLine(good.Name);
            }

            if (goods.Count == 0) 
            {
                await response.UpdateAsync(x => {
                    x.Content = $"{_province.Name} has no Goods which can be removed.";
                    x.Components = null;
                });
            }

            sm = _interactionHelpers.CreateSelectMenu(messageId.ToString(), "GoodSelection", goods, "Select Goods");
            sm.MinValues = 1;
            sm.MaxValues = sm.Options.Count();
            Console.WriteLine("Building Menu");
            builder = new ComponentBuilder()
                .WithSelectMenu(sm);

            await response.UpdateAsync(x => {
                x.Content = "Select Goods!";
                x.Components = builder.Build();
            });

            Predicate<SocketInteraction> GoodSelection = s =>
            {
                if (s.GetType() != typeof(SocketMessageComponent)) return false;
                SocketMessageComponent d = (SocketMessageComponent)s;
                return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId.ToString(), "GoodSelection");
            };

            response = (SocketMessageComponent)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 1, 0), GoodSelection);

            foreach (string good in response.Data.Values)
            {
                Modifier goodModifier = _province.Modifiers.FirstOrDefault(m => m.Name == good);
                if (goodModifier == null)
                {
                    await RespondAsync($"{good} not found", ephemeral: true);
                    return;
                }
                db.Modifiers.Remove(goodModifier);
            }

            await db.SaveChangesAsync();
            await response.UpdateAsync(x => {
                x.Content = "Success!";
                x.Components = null;
            });
        }

        [SlashCommand("show", "Displays Information about a good")]
        public async Task ShowGood(string province)
        {
            using var db = new IcarusContext();

            int messageId = Random.Shared.Next(0, 9999);
            List<string> modifiers = new List<string>();
            Province _province = null;
            if (province == null)
            {
                await RespondAsync("Province not found!", ephemeral: true);
                return;
            }
            else
            {
                _province = db.Provinces.FirstOrDefault(p => p.Name == province);
                if (_province == null)
                {
                    await RespondAsync("Province not found!", ephemeral: true);
                    return;
                }


                foreach (Modifier modifier in _province.Modifiers.Where(m => m.isGood == true))
                {
                    modifiers.Add(modifier.Name);
                    Debug.WriteLine(modifier.Name);
                }
            }

            if (modifiers.Count == 0)
            {
                await RespondAsync($"{province} has no Good which can be displayed.", ephemeral: true);
            }

            SelectMenuBuilder sm = _interactionHelpers.CreateSelectMenu(messageId.ToString(), "ModifierSelection", modifiers, "Select Modifier");
            sm.MinValues = 1;
            sm.MaxValues = 1;
            Console.WriteLine("Building Menu");
            ComponentBuilder builder = new ComponentBuilder()
                .WithSelectMenu(sm);

            await RespondAsync("Choose Good", components: builder.Build(), ephemeral: true);

            Predicate<SocketInteraction> GoodSelection = s =>
            {
                if (s.GetType() != typeof(SocketMessageComponent)) return false;
                SocketMessageComponent d = (SocketMessageComponent)s;
                return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId.ToString(), "ModifierSelection");
            };

            SocketMessageComponent response = (SocketMessageComponent)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 1, 0), GoodSelection);

            if (response == null) { return; }

            Modifier Modifier = _province.Modifiers.FirstOrDefault(m => m.Name == response.Data.Values.First());

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
                Title = $"{_valueManagementService.GetDescFromLevel(Modifier.Level)} {Modifier.Name} Industry",
                Description = Modifier.Description,
            };

            StringBuilder Effects = new StringBuilder();
            foreach (ValueModifier vm in Modifier.Modifiers)
            {
                Effects.AppendLine($"{vm.ValueTag}:{Math.Round(vm.Modifier * _valueManagementService.GetModifierFromLevel(Modifier.Level),2)}");
                if (vm.Decay != 0)
                {
                    Effects.Append($" Decay: {vm.Decay}");
                }
            }
            if (Modifier.Modifiers.Count == 0)
            {
                Effects.Append("None");
            }

            emb.AddField("Effects", Effects.ToString());

            await response.UpdateAsync(x => {
                x.Content = $"Showing Modifier {Modifier.Name}";
                x.Components = null;
                x.Embed = emb.Build();
            });
        }

        [SlashCommand("add-level", "Increases the level of a good by one.")]
        [RequireAdmin]
        public async Task AddGoodLevel()
        {
            using var db = new IcarusContext();

            List<string> provinces = new List<string>();
            foreach (Province province in db.Provinces.Where(p => p.Modifiers.Any(m => m.isGood == true)))
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

            Province _province = db.Provinces.FirstOrDefault(p => p.Name == response.Data.Values.First());
            if (_province == null)
            {
                await RespondAsync("Province not found!");
                return;
            }

            List<string> goods = new List<string>();
            foreach (Modifier good in _province.Modifiers.Where(m => m.isGood == true))
            {
                goods.Add(good.Name);
                Debug.WriteLine(good.Name);
            }

            if (goods.Count == 0)
            {
                await response.UpdateAsync(x => {
                    x.Content = $"{_province.Name} has no Goods which can be removed.";
                    x.Components = null;
                });
            }

            sm = _interactionHelpers.CreateSelectMenu(messageId.ToString(), "GoodSelection", goods, "Select Good");
            sm.MinValues = 1;
            sm.MaxValues = 1;
            Console.WriteLine("Building Menu");
            builder = new ComponentBuilder()
                .WithSelectMenu(sm);

            await response.UpdateAsync(x => {
                x.Content = "Select Goods!";
                x.Components = builder.Build();
            });

            Predicate<SocketInteraction> GoodSelection = s =>
            {
                if (s.GetType() != typeof(SocketMessageComponent)) return false;
                SocketMessageComponent d = (SocketMessageComponent)s;
                return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId.ToString(), "GoodSelection");
            };

            response = (SocketMessageComponent)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 1, 0), GoodSelection);

            Modifier Good = _province.Modifiers.FirstOrDefault(m => m.Name == response.Data.Values.First());
            Level old = Good.Level;
            Good.Level = _valueManagementService.GetNextLevel(Good.Level);
            await db.SaveChangesAsync();
            await response.UpdateAsync(x => {
                x.Content = $"Increased from {_valueManagementService.GetDescFromLevel(old)} {Good.Name} Industry to {_valueManagementService.GetDescFromLevel(Good.Level)} {Good.Name} Industry";
                x.Components = null;
            });
        }

        [SlashCommand("set-level", "Increases the level of a good by one.")]
        [RequireAdmin]
        public async Task SetGoodLevel(
            [Summary("Good-Level", "The level of the good."),
            Choice("Miniscule", "Miniscule"),
            Choice("Small", "Small"),
            Choice("Normal", "Normal"),
            Choice("Large", "Large"),
            Choice("Massive", "Massive"),]string level)
        {
            Level _level = (Level)Enum.Parse(typeof(Level), level);
            using var db = new IcarusContext();

            List<string> provinces = new List<string>();
            foreach (Province province in db.Provinces.Where(p => p.Modifiers.Any(m => m.isGood == true)))
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

            Province _province = db.Provinces.FirstOrDefault(p => p.Name == response.Data.Values.First());
            if (_province == null)
            {
                await RespondAsync("Province not found!");
                return;
            }

            List<string> goods = new List<string>();
            foreach (Modifier good in _province.Modifiers.Where(m => m.isGood == true))
            {
                goods.Add(good.Name);
                Debug.WriteLine(good.Name);
            }

            if (goods.Count == 0)
            {
                await response.UpdateAsync(x => {
                    x.Content = $"{_province.Name} has no Goods which can be removed.";
                    x.Components = null;
                });
            }

            sm = _interactionHelpers.CreateSelectMenu(messageId.ToString(), "GoodSelection", goods, "Select Good");
            sm.MinValues = 1;
            sm.MaxValues = 1;
            Console.WriteLine("Building Menu");
            builder = new ComponentBuilder()
                .WithSelectMenu(sm);

            await response.UpdateAsync(x => {
                x.Content = "Select Goods!";
                x.Components = builder.Build();
            });

            Predicate<SocketInteraction> GoodSelection = s =>
            {
                if (s.GetType() != typeof(SocketMessageComponent)) return false;
                SocketMessageComponent d = (SocketMessageComponent)s;
                return d.Data.CustomId == _interactionHelpers.GenerateCompoundId(messageId.ToString(), "GoodSelection");
            };

            response = (SocketMessageComponent)await InteractionUtility.WaitForInteractionAsync(_client, new TimeSpan(0, 1, 0), GoodSelection);

            Modifier Good = _province.Modifiers.FirstOrDefault(m => m.Name == response.Data.Values.First());
            Level old = Good.Level;
            Good.Level = _level;
            await db.SaveChangesAsync();
            await response.UpdateAsync(x => {
                x.Content = $"Changed from {_valueManagementService.GetDescFromLevel(old)} {Good.Name} Industry to {_valueManagementService.GetDescFromLevel(Good.Level)} {Good.Name} Industry";
                x.Components = null;
            });
        }
    }
}
