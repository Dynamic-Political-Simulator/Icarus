﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Discord;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Discord.EconCommands;
using Icarus.Utils;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;

namespace Icarus.Services
{
    public class ValueManagementService
    {
        private readonly TickService _tickService;
        private readonly EconVisualsService _visualsService;
        private readonly DebugService _debugService;
        public ValueManagementService(TickService tickService, EconVisualsService econVisualsService, DebugService debugService)
        {
            _tickService = tickService;
            _visualsService = econVisualsService;

            _tickService.TickEvent += ValueTick;
            _debugService = debugService;
        }

        /// <summary>
        /// Wrapper Function which executes Tick Logic every Tick
        /// </summary>
        public async Task ValueTick()
        {
            try
            {
                await UpdateValues();
                await UpdateModifiers();
                Console.WriteLine("Values Ticked! Generating Sheets!");
                await _visualsService.UpdateProvinceView(this);
                Console.WriteLine("Updated Sheets!");
            }
            catch (Exception ex)
            {
                _ = _debugService.PrintToChannels(ex.ToString());
            }
            

        }

        /// <summary>
        /// This updates all Values.
        /// It first saves all projected changes into a cache and then applies them all during a second loop
        /// </summary>
        /// <returns>Nothing</returns>
        /// <exception cref="Exception">This shouldn't be possible but lets have it here just in case</exception>
        public async Task UpdateValues()
        {
            using var db = new IcarusContext();

            List<ValueChangeCache> changes = new List<ValueChangeCache>();

            foreach (Value Value in db.Values)
            {
                changes.Add(new ValueChangeCache()
                {
                    ValueId = Value.Id,
                    ValueChange = GetValueChange(Value),
                    ValueGoal = GetValueGoal(Value)
                });
                //Console.WriteLine($"{Value.Name}:{Value.CurrentValue}");
            }
            foreach (ValueChangeCache change in changes)
            {
                Value value = db.Values.FirstOrDefault(v => v.Id== change.ValueId);
                if (value == null)
                {
                    throw new Exception("During the Tick ValueId was saved but later not found again. This should be impossible what did you do?");
                }
                else
                {
                    CalculateNewValue(value, change.ValueChange, change.ValueGoal);
                }
            }
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Projects the change of a Value for the next tick.
        /// </summary>
        /// <param name="value">Value to calculate the change for.</param>
        /// <returns>The projected Change of the given Value.</returns>
        public float GetValueChange(Value value)
        {
            var config = ConfigFactory.GetConfig();

            float ValueGoal = GetValueGoal(value);
            if (ValueGoal == value.CurrentValue)
            {
                return 0;
            }
            float Change;
            if (value.CurrentValue < ValueGoal) //The change needs to be positive
            {
                Change = 0.2f + ((ValueGoal - value.CurrentValue) * config.ValueChangeRatio);
                //If the change would move us above the goal then just change by enough to give us the exact Goal
                if ((value.CurrentValue + Change) > ValueGoal)
                { 
                    Change = ValueGoal - value.CurrentValue;
                }
            }
            else //The change needs to be negative
            {
                //(neg) = (neg)   + (neg)
                Change = (-0.5f) + ((ValueGoal - value.CurrentValue) * config.ValueChangeRatio);
                //If the change would move us below the goal then just change by enough to give us the exact Goal
                if ((value.CurrentValue + Change) < ValueGoal)
                {
                    Change = ValueGoal - value.CurrentValue;
                }
            }

            return (float)Math.Round(Change,2);

            //return AggregateModifiers(value) + value.RelationInducedChange;
        }

        /// <summary>
        /// Calculate the Goal a Value to moving to.
        /// </summary>
        /// <param name="value">Value to calculate the Goal for.</param>
        /// <returns>The Goal a Value is moving to</returns>
        public float GetValueGoal(Value value)
        {
            //Goal due to Modifers + Goal due to Relationships + Base
            float goal = (float)Math.Round(AggregateModifiers(value) + ReEvaluateRelations(value) + value.BaseBalue, 2);
            if (goal < 0)
            {
                return 0f;
            }
            return goal;
        }

        /// <summary>
        /// Generate a List of the origin of the varios Modifiers on a Goal
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public Dictionary<string, float> GetValueGoalDesc(Value Value)
        {
            Dictionary<string,float> desc = new Dictionary<string,float>();
            desc.Add("Base", Value.BaseBalue);
            ValueModifier vm;
            foreach (Modifier modifier in Value.Province.Modifiers.Where(m => m.Modifiers.FirstOrDefault(vm => vm.ValueTag == Value.TAG) != null))
            {
                vm = modifier.Modifiers.FirstOrDefault(vm => vm.ValueTag == Value.TAG);
                float LevelModifier = 1f;
                if (modifier.isGood == true)
                {
                    LevelModifier = GetModifierFromLevel(modifier.Level);
                    desc.Add($"{GetDescFromLevel(modifier.Level)} {modifier.Name} Industry", (float)Math.Round(vm.Modifier * LevelModifier, 2));
                    continue;
                }
                desc.Add(modifier.Name, vm.Modifier);

            }
            //Aggregate the global Modifiers
            foreach (Modifier modifier in Value.Province.Nation.Modifiers.Where(m => m.Modifiers.FirstOrDefault(vm => vm.ValueTag == Value.TAG) != null))
            {
                vm = modifier.Modifiers.FirstOrDefault(vm => vm.ValueTag == Value.TAG);
                
                desc.Add($"{modifier.Name}", (float)Math.Round(vm.Modifier,2));
            }

            using var db = new IcarusContext();

            //First float is Value second float is Weight
            //Tuple Value1 = Current Value ; Value2 = Weighting
            List<Tuple<float, float, string>> ValueWeightPair = new List<Tuple<float, float, string>>();
            float TotalWeight = 0f;

            // Added AsQueryable here because it bitched about the Where being ambiguous
            //List of every relationship where the current Value is the Target
            List<ValueRelationship> Relationships = db.Relationships.AsQueryable().Where(vr => vr.TargetTag == Value.TAG).ToList();
            foreach (ValueRelationship relationship in Relationships)
            {
                Value origin = Value.Province.Values.FirstOrDefault(v => v.TAG == relationship.OriginTag);
                ValueWeightPair.Add(new Tuple<float, float,string>(origin.CurrentValue, relationship.Weight,relationship.OriginTag));
                TotalWeight += Math.Abs(relationship.Weight);
            }

            foreach (Tuple<float, float, string> tuple in ValueWeightPair)
            {
                var goal = (tuple.Item1 * tuple.Item2) / TotalWeight;
                desc.Add(tuple.Item3, (float)Math.Round(goal, 2));
            }

            return desc;
        }

        /// <summary>
        /// Apply a Change to a Value. Value can't be smaller than 0. Value is rounded to the nearest two digits.
        /// </summary>
        /// <param name="Value">Value to change</param>
        /// <param name="ValueChange">Change of the Value</param>
        /// <returns>New CurrentValue of the Value</returns>
        public float CalculateNewValue(Value Value, float ValueChange, float Goal)
        {
            float newValue = Value.CurrentValue + ValueChange;
            //Add the value to the history
            ValueHistory valueHistory = new ValueHistory() 
            {
                Height = Value.CurrentValue,
                Goal = Goal,
                Change = ValueChange
            };
            Value.PastValues.Add(valueHistory);
            if (Value.PastValues.Count > 1000)
            {
                Value.PastValues.RemoveAt(0);
            }
            Value.CurrentValue = (float)Math.Round(newValue,2);
            if (Value.CurrentValue < 0) { Value.CurrentValue = 0; }
            return Value.CurrentValue;
        }   

        /// <summary>
        /// Convert the enum Level to a float modifier.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public float GetModifierFromLevel (Level level)
        {
            switch (level)
            {
                case Level.Miniscule: return 0.25f;
                case Level.Small: return 0.5f;
                case Level.Normal: return 1f;
                case Level.Large: return 2f;
                case Level.Massive: return 4f;
                default: return 1f;
            }
        }

        /// <summary>
        /// Provide the description string for the enum level
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public string GetDescFromLevel(Level level)
        {
            switch (level)
            {
                case Level.Miniscule: return "Miniscule";
                case Level.Small: return "Small";
                case Level.Normal: return "";
                case Level.Large: return "Large";
                case Level.Massive: return "Massive";
                default: return "";
            }
        }

        public Level GetNextLevel(Level level)
        {
            switch (level)
            {
                case Level.Miniscule: return Level.Small;
                case Level.Small: return Level.Normal;
                case Level.Normal: return Level.Large;
                case Level.Large: return Level.Massive;
                case Level.Massive: return Level.Massive;
                default: return Level.Normal;
            }
        }

        /// <summary>
        /// Combine all modifiers affecting a certain Value.
        /// </summary>
        /// <param name="Value">Value to aggregate modifiers for.</param>
        /// <returns>The combined shift in Goal of all modifiers</returns>
        public float AggregateModifiers(Value Value)
        {
            float Total = 0f;
            ValueModifier vm;
            //Aggregate the local Modifiers
            //The loop iterates over all Modifiers which contain a ValueModifier targeting the chosen Value
            foreach (Modifier modifier in Value.Province.Modifiers.Where(m => m.Modifiers.FirstOrDefault(vm => vm.ValueTag == Value.TAG) != null)) 
            {
                vm = modifier.Modifiers.FirstOrDefault(vm => vm.ValueTag == Value.TAG);
                float LevelModifier = 1f;
                if ( modifier.isGood == true)
                {
                    LevelModifier = GetModifierFromLevel(modifier.Level);
                }
                Total += vm.Modifier * LevelModifier;
            }
            //Aggregate the global Modifiers
            foreach (Modifier modifier in Value.Province.Nation.Modifiers.Where(m => m.Modifiers.FirstOrDefault(vm => vm.ValueTag == Value.TAG) != null))
            {
                vm = modifier.Modifiers.FirstOrDefault(vm => vm.ValueTag == Value.TAG);
                Total += vm.Modifier;
            }

            return Total;
        }

        /// <summary>
        /// Calculate the Goal of a Value induced only by its Relations.
        /// </summary>
        /// <param name="value">Value to calculate the Goal for</param>
        /// <returns>Goal induced by relations of the given Value</returns>
        public float ReEvaluateRelations(Value value)
        {
            using var db = new IcarusContext();

            float NewGoal = 0f;

            
            //First float is Value second float is Weight
            //Tuple Value1 = Current Value ; Value2 = Weighting
            List<Tuple<float,float>> ValueWeightPair= new List<Tuple<float,float>>();
            float TotalWeight = 0f;

            // Added AsQueryable here because it bitched about the Where being ambiguous
            //List of every relationship where the current Value is the Target
            List<ValueRelationship> Relationships = db.Relationships.AsQueryable().Where(vr => vr.TargetTag == value.TAG).ToList();
            if (Relationships.Count == 0) 
            {
                return 0f;
            }
            foreach(ValueRelationship relationship in Relationships)
            {
                Value origin = value.Province.Values.FirstOrDefault(v => v.TAG == relationship.OriginTag);
                ValueWeightPair.Add(new Tuple<float, float>(origin.CurrentValue, relationship.Weight));
                TotalWeight += Math.Abs(relationship.Weight);
            }

            foreach(Tuple<float,float> tuple in ValueWeightPair)
            {
                NewGoal += (tuple.Item1 * tuple.Item2) / TotalWeight;
            }
            string name = value.Name;
            return NewGoal;
        }


        /// <summary>
        /// This adjusts the remaining time and height of Modifiers after a tick.
        /// </summary>
        /// <returns>Nothing</returns>
        public async Task UpdateModifiers()
        {
            using var db = new IcarusContext();
            List<Modifier> MarkedForRemovalModifiers = new List<Modifier>();
            List<ValueModifier> ValueModifiersMarkedForRemoval = new List<ValueModifier>();
            foreach (Modifier modifier in db.Modifiers)
            {
                switch (modifier.Type)
                {
                    case ModifierType.Temporary:
                        modifier.Duration -= 1;
                        if (modifier.Duration == 0)
                        {
                            MarkedForRemovalModifiers.Add(modifier);
                            //db.Modifiers.Remove(modifier);
                        }
                        break;
                    case ModifierType.Decaying:
                        foreach(ValueModifier vm in modifier.Modifiers)
                        {
                            if (vm.Modifier > 0)
                            {
                                vm.Modifier += vm.Decay;
                                Math.Round(vm.Modifier, 2);
                                if (vm.Modifier <= 0)
                                {
                                    //modifier.Modifiers.Remove(vm);
                                    //db.ValueModifiers.Remove(vm);
                                    ValueModifiersMarkedForRemoval.Add(vm);
                                }
                            }
                            else
                            {
                                vm.Modifier += vm.Decay;
                                Math.Round(vm.Modifier,2);
                                if (vm.Modifier >= 0)
                                {
                                    //modifier.Modifiers.Remove(vm);
                                    //db.ValueModifiers.Remove(vm);
                                    ValueModifiersMarkedForRemoval.Add(vm);
                                }
                            }
                        }

                        foreach(ValueModifier vm in ValueModifiersMarkedForRemoval)
                        {
                            modifier.Modifiers.Remove(vm);
                            db.ValueModifiers.Remove(vm);
                        }

                        ValueModifiersMarkedForRemoval = new List<ValueModifier>();

                        if (modifier.Modifiers.Count == 0)
                        {
                            MarkedForRemovalModifiers.Add(modifier);
                            //db.Modifiers.Remove(modifier);
                        }
                        break;
                }
            }

            foreach(var modifier in MarkedForRemovalModifiers)
            {
                db.Modifiers.Remove(modifier);
            }

            await db.SaveChangesAsync();
        }

        public Province AddGoodToProvince(Good good, Province province)
        {
            Modifier GoodModifier = new Modifier()
            {
                Name = good.Name,
                Description = good.Description,
                Type = ModifierType.Permanent,
                isGood = true,
                WealthMod = good.WealthMod,
            };

            foreach (GoodValueModifier vm in good.ValueModifiers)
            {
                GoodModifier.Modifiers.Add(new ValueModifier()
                {
                    Modifier = vm.Modifier,
                    ValueTag = vm.ValueTag,
                    Decay = vm.Decay
                });
            }

            province.Modifiers.Add(GoodModifier);

            return province;
        }

        /// <summary>
        /// Depreceated Function to generate Values. DO NOT USE!
        /// </summary>
        /// <returns>List of Value Objects</returns>
        public List<Value> GenerateValueTemplate()
        {
            List<Value> Values = new List<Value>();
            Value StandardOfLiving = new Value() {
                Name= "StandardOfLiving",
                CurrentValue = 10,
            };
            Values.Add(StandardOfLiving);
            Value Literacy = new Value()
            {
                Name = "Literacy",
                CurrentValue = 10,
            };
            Values.Add(Literacy);
            Value Security = new Value()
            {
                Name = "Security",
                CurrentValue = 10,
            };
            Values.Add(Security);
            Value Health = new Value()
            {
                Name = "Health",
                CurrentValue = 10,
            };
            Values.Add(Health);
            Value Housing = new Value()
            {
                Name = "Housing",
                CurrentValue = 10,
            };
            Values.Add(Housing);
            Value Urbanization = new Value()
            {
                Name = "Urbanization",
                CurrentValue = 10,
            };
            Values.Add(Urbanization);
            Value EconomicStrength = new Value()
            {
                Name = "Economic Strength",
                CurrentValue = 10,
            };
            Values.Add(EconomicStrength);
            Value Mechanisation = new Value()
            {
                Name = "Mechanisation",
                CurrentValue = 10,
            };
            Values.Add(Mechanisation);
            Value IndustrialOutput = new Value()
            {
                Name = "IndustrialOutput",
                CurrentValue = 10,
            };
            Values.Add(IndustrialOutput);
            Value Infrastructure = new Value()
            {
                Name = "Infrastructure",
                CurrentValue = 10,
            };
            Values.Add(Infrastructure);
            Value Destitution = new Value()
            {
                Name = "Destitution",
                CurrentValue = 10,
            };
            Values.Add(Destitution);


            return Values;
        }

        //-----------------------------------------
        //All the Gamestate XML reading after this
        //-----------------------------------------

        /// <summary>
        /// Wrapper function which handles top level reading logic.
        /// </summary>
        /// <param name="gameState">Gamestate to add game info to.</param>
        /// <returns>nothing</returns>
        public async Task ReadGameStateConfig(GameState gameState, ISocketMessageChannel channel)
        {
            string DataPath = @"./GameStateConfig.xml";
            XmlDocument Xmldata = new XmlDocument();
            Xmldata.Load(DataPath);
            XmlNode xmlNode = Xmldata.LastChild.SelectSingleNode("Nation");
            await channel.SendMessageAsync($"Generating Good Templates!");
            await ReadGoodXml(Xmldata.LastChild.SelectSingleNode("Goods"));
            await channel.SendMessageAsync($"Done with Good Templates!");
            await ReadNation(gameState, Xmldata.LastChild.SelectSingleNode("Nation"), Xmldata.LastChild.SelectSingleNode("ValueTemplates"), channel);
            await channel.SendMessageAsync($"Generating Value Relationships!");
            await GenerateValueRelationships(Xmldata.LastChild.SelectSingleNode("ValueRelationShips"));
            await channel.SendMessageAsync($"Everything successfully done!");
        }

        /// <summary>
        /// Reads all relevant GameState Data from an XML.
        /// </summary>
        /// <param name="gameState">Gamestate to add nation to.</param>
        /// <param name="NationXml">XML Node containing Nation Data</param>
        /// <param name="ValueTemplatesXml">XML Node containing generic Value Data</param>
        /// <returns></returns>
        public async Task ReadNation(GameState gameState, XmlNode NationXml, XmlNode ValueTemplatesXml, ISocketMessageChannel channel)
        {
            using var db = new IcarusContext();

            Nation NewNation = new Nation(){
                Name = NationXml.SelectSingleNode("Name").InnerText,
                Description = NationXml.SelectSingleNode("Description").InnerText
            };
            await channel.SendMessageAsync($"Nation {NewNation.Name} created!");
            Console.WriteLine($"Nation {NewNation.Name} created!");
            List<ValueTemplate> ValueTemplates = new List<ValueTemplate>();

            Console.WriteLine("Starting Value Registration!");
            await channel.SendMessageAsync("Starting Value Registration!");
            foreach (XmlNode ValueTemplate in ValueTemplatesXml)
            {
                ValueTemplates.Add(new ValueTemplate()
                {
                    Name = ValueTemplate.SelectSingleNode("Name").InnerText,
                    Description = ValueTemplate.SelectSingleNode("Description").InnerText,
                    TAG = ValueTemplate.SelectSingleNode("Tag").InnerText
                });
                Console.WriteLine($"Value {ValueTemplate.SelectSingleNode("Name").InnerText} with tag {ValueTemplate.SelectSingleNode("Tag").InnerText} registered!");
            }
            Console.WriteLine("Value Registration done!");
            await channel.SendMessageAsync("Value Registration done!");
            Console.WriteLine("Registering Provinces!");
            await channel.SendMessageAsync("Registering Provinces!");
            foreach (XmlNode province in NationXml.SelectSingleNode("Provinces").ChildNodes)
            {
                Province NewProvince = new Province()
                {
                    Name = province.SelectSingleNode("Name").InnerText,
                    Description = province.SelectSingleNode("Description").InnerText
                };
                await channel.SendMessageAsync($"Creating Province {NewProvince.Name}!");
                foreach (XmlNode Value in province.SelectSingleNode("Values").ChildNodes)
                {
                    ValueTemplate vTemplate = ValueTemplates.FirstOrDefault(v => v.TAG == Value.Name);
                    float basev = 0f;
                    if (Value.Name == "BCY"|| Value.Name == "I"|| Value.Name == "U"|| Value.Name == "M" ||
                        Value.Name == "H" || Value.Name == "SEC" || Value.Name == "EDU" || Value.Name == "WEL")
                    {
                        basev = float.Parse(Value.InnerText, System.Globalization.CultureInfo.InvariantCulture);
                    }

                    NewProvince.Values.Add(new Value()
                    {
                        Name = vTemplate.Name,
                        Description = vTemplate.Description,
                        TAG = vTemplate.TAG,
                        BaseBalue = basev,
                        CurrentValue = float.Parse(Value.InnerText, System.Globalization.CultureInfo.InvariantCulture)
                    });
                }
                foreach(XmlNode Modifier in province.SelectSingleNode("Modifiers").ChildNodes)
                {
                    Modifier NewModifier = new Modifier()
                    {
                        Name = Modifier.SelectSingleNode("Name").InnerText,
                        Description = Modifier.SelectSingleNode("Description").InnerText,
                        Type = (ModifierType)Enum.Parse(typeof(ModifierType), Modifier.SelectSingleNode("Type").InnerText),
                        Duration = int.Parse(Modifier.SelectSingleNode("Duration").InnerText),
                        WealthMod = float.Parse(Modifier.SelectSingleNode("WealthMod").InnerText, System.Globalization.CultureInfo.InvariantCulture)
                    };

                    foreach(XmlNode ValueModifier in Modifier.SelectSingleNode("Values").ChildNodes)
                    {
                        NewModifier.Modifiers.Add(new Context.Models.ValueModifier()
                        {
                            ValueTag = ValueModifier.SelectSingleNode("Tag").InnerText,
                            Modifier = float.Parse(ValueModifier.SelectSingleNode("Modifier").InnerText),
                            Decay = float.Parse(ValueModifier.SelectSingleNode("Decay").InnerText)
                        });
                    }
                    
                    NewProvince.Modifiers.Add(NewModifier);
                }

                foreach(XmlNode GoodTag in province.SelectSingleNode("Goods").ChildNodes)
                {
                    Good GoodTemplate = db.Goods.FirstOrDefault(g => g.TAG == GoodTag.InnerText);
                    if (GoodTemplate == null)
                    {
                        throw new Exception("Province contains Good Tag that can not be found in db.");
                    }
                    else
                    {
                        AddGoodToProvince(GoodTemplate, NewProvince);
                    }
                }

                await channel.SendMessageAsync($"Done with Province {NewProvince.Name}!");
                NewNation.Provinces.Add(NewProvince);
            }

            gameState.Nation= NewNation;
            db.Update(gameState);
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Creates the Relationships between Values from an XML
        /// </summary>
        /// <param name="RelationshipsXml">XML node containing Relationship data.</param>
        /// <returns>Nothing</returns>
        public async Task GenerateValueRelationships(XmlNode RelationshipsXml)
        {
            using var db = new IcarusContext();

            List<Value> Values = db.Values.ToList();

            List<RelationShipDTO> RelationShipDTOs = new List<RelationShipDTO>();
            foreach (XmlNode relationship in RelationshipsXml.ChildNodes)
            {
                RelationShipDTOs.Add(new RelationShipDTO()
                {
                    Origin = relationship.SelectSingleNode("Origin").InnerText,
                    Target = relationship.SelectSingleNode("Target").InnerText,
                    Weight = float.Parse(relationship.SelectSingleNode("Weight").InnerText),
                });

                db.Relationships.Add(new ValueRelationship()
                {
                    OriginTag = relationship.SelectSingleNode("Origin").InnerText,
                    TargetTag = relationship.SelectSingleNode("Target").InnerText,
                    Weight = float.Parse(relationship.SelectSingleNode("Weight").InnerText),
                });
            }

            List<ValueRelationship> Relationships = new List<ValueRelationship>();

            /*foreach(Value Value in Values)
            {
                List<RelationShipDTO> ValidDTOs = RelationShipDTOs.Where(v=> v.Origin == Value.TAG).ToList();
                foreach (RelationShipDTO DTO in ValidDTOs)
                {
                    Value Target = Values.FirstOrDefault(v => v.TAG == DTO.Target);
                    if (Target != null)
                    {
                        Relationships.Add(new ValueRelationship()
                        {
                            Origin = Value,
                            Target = Target,
                            Weight = DTO.Weight
                        });
                    }
                    else
                    {
                        //Crash
                        Console.WriteLine("SOMETHING HAS GONE WRONG. THERE WAS A RELATIONSHIP FOUND WITH AN EXISTING ORIGIN VALUE BUT A MISSING TARGET VALUE");
                    }
                }
            }

            db.Relationships.AddRange(Relationships);*/

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Read Goods from XML
        /// </summary>
        /// <param name="GoodsXml">XML Node containing Good data</param>
        /// <returns>Nothing</returns>
        public async Task ReadGoodXml(XmlNode GoodsXml)
        {
            using var db = new IcarusContext();

            foreach (XmlNode Modifier in GoodsXml.ChildNodes)
            {
                Good NewGood = new Good()
                {
                    Name = Modifier.SelectSingleNode("Name").InnerText,
                    Description = Modifier.SelectSingleNode("Description").InnerText,
                    TAG = Modifier.SelectSingleNode("TAG").InnerText,
                    WealthMod = float.Parse(Modifier.SelectSingleNode("WealthMod").InnerText, System.Globalization.CultureInfo.InvariantCulture)
                };

                foreach (XmlNode ValueModifier in Modifier.SelectSingleNode("Values").ChildNodes)
                {
                    NewGood.ValueModifiers.Add(new Context.Models.GoodValueModifier()
                    {
                        ValueTag = ValueModifier.SelectSingleNode("Tag").InnerText,
                        Modifier = float.Parse(ValueModifier.SelectSingleNode("Modifier").InnerText),
                        Decay = 0
                    });
                }
                Good good = db.Goods.FirstOrDefault(x => x.TAG == NewGood.TAG);
                if (good != null)
                {
                    good = NewGood;
                }
                else
                {
                    db.Goods.Add(NewGood);
                }
                
            }
            await db.SaveChangesAsync();
        }
    }


    //Some Helper Objects
    public class RelationShipDTO
    {
        public string Origin { get; set; }
        public string Target { get; set; }
        public float Weight { get; set; }
    }

    public class ValueTemplate
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string TAG { get; set; }
    }

    public class ValueChangeCache
    {
        public int ValueId { get; set; }
        public float ValueChange { get; set; }
        public float ValueGoal { get; set; }
    }
}
