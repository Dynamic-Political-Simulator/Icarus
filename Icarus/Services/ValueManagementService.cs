using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Discord.EconCommands;
using Icarus.Utils;
using Microsoft.Identity.Client;

namespace Icarus.Services
{
    public class ValueManagementService
    {
        private readonly TickService _tickService;
        public List<ModifierCreationDTO> Modifiers { get; set; } = new List<ModifierCreationDTO>();

        public ValueManagementService(TickService tickService)
        {
            _tickService = tickService;

            _tickService.TickEvent += ValueTick;
        }

        //This will need to be executed every Tick
        public async void ValueTick()
        {
            await UpdateValues();
            await UpdateModifiers();

        }
        public async Task UpdateValues()
        {
            using var db = new IcarusContext();

            foreach (Value Value in db.Values)
            {
                CalculateNewValue(Value);
            }
            await db.SaveChangesAsync();
        }

        public float GetValueChange(Value value)
        {
            var config = ConfigFactory.GetConfig();

            float ValueGoal = GetValueGoal(value);
            float Change;
            if (value.CurrentValue < ValueGoal)
            {
                Change = 0.5f + ((value.CurrentValue - ValueGoal) * config.ValueChangeRatio);
                //If the change would move us above the goal then just change by enough to give us the exact Goal
                if ((value.CurrentValue + Change) > ValueGoal)
                { 
                    Change = ValueGoal - value.CurrentValue;
                }
            }
            else
            {
                Change = (0.5f + ((value.CurrentValue - ValueGoal) * config.ValueChangeRatio))*-1;
                //If the change would move us below the goal then just change by enough to give us the exact Goal
                if ((value.CurrentValue + Change) < ValueGoal)
                {
                    Change = ValueGoal - value.CurrentValue;
                }
            }

            return (float)Math.Round(Change,2);

            //return AggregateModifiers(value) + value.RelationInducedChange;
        }

        public float GetValueGoal(Value value)
        {
            return AggregateModifiers(value) + ReEvaluateRelations(value);
        }

        public float CalculateNewValue(Value Value)
        {
            float ValueChange = GetValueChange(Value);
            Value.CurrentValue += ValueChange;
            return Value.CurrentValue;
        }

        public float AggregateModifiers(Value Value)
        {
            float Total = 0f;
            ValueModifier vm;
            //Aggregate the local Modifiers
            foreach (Modifier modifier in Value.Province.Modifiers.Where(m => m.Modifiers.FirstOrDefault(vm => vm.ValueTag == Value.TAG) != null)) 
            {
                vm = modifier.Modifiers.FirstOrDefault(vm => vm.ValueTag == Value.TAG);
                Total += vm.Modifier;
            }
            //Aggregate the global Modifiers
            foreach (Modifier modifier in Value.Province.Nation.Modifiers.Where(m => m.Modifiers.FirstOrDefault(vm => vm.ValueTag == Value.TAG) != null))
            {
                vm = modifier.Modifiers.FirstOrDefault(vm => vm.ValueTag == Value.TAG);
                Total += vm.Modifier;
            }

            return Total;
        }

        public float ReEvaluateRelations(Value value)
        {
            using var db = new IcarusContext();

            float NewGoal = 0f;

            
            //First float is Value second float is Weight
            List<Tuple<float,float>> ValueWeightPair= new List<Tuple<float,float>>();
            float TotalWeight = 0f;

            // Added AsQueryable here because it bitched about the Where being ambiguous
            //List of every relationship where the current Value is the Target
            List<ValueRelationship> Relationships = db.Relationships.AsQueryable().Where(vr => vr.Target == value).ToList();
            foreach(ValueRelationship relationship in Relationships)
            {
                ValueWeightPair.Add(new Tuple<float, float>(relationship.Origin.CurrentValue, relationship.Weight));
                TotalWeight += Math.Abs(relationship.Weight);
            }

            foreach(Tuple<float,float> tuple in ValueWeightPair)
            {
                NewGoal += (tuple.Item1 * tuple.Item2) / TotalWeight;
            }

            return NewGoal;
        }



        public async Task UpdateModifiers()
        {
            using var db = new IcarusContext();

            foreach (Modifier modifier in db.Modifiers)
            {
                switch (modifier.Type)
                {
                    case ModifierType.Temporary:
                        modifier.Duration -= 1;
                        if (modifier.Duration == 0)
                        {
                            db.Modifiers.Remove(modifier);
                        }
                        break;
                    case ModifierType.Decaying:
                        foreach(ValueModifier vm in modifier.Modifiers)
                        {
                            if (vm.Decay > 0)
                            {
                                vm.Modifier += vm.Decay;
                                if (vm.Modifier > 0)
                                {
                                    modifier.Modifiers.Remove(vm);
                                    db.ValueModifiers.Remove(vm);
                                }
                            }
                            else
                            {
                                vm.Modifier -= vm.Decay;
                                if (vm.Modifier < 0)
                                {
                                    modifier.Modifiers.Remove(vm);
                                    db.ValueModifiers.Remove(vm);
                                }
                            }
                        }
                        if (modifier.Modifiers.Count == 0)
                        {
                            db.Modifiers.Remove(modifier);
                        }
                        break;
                }
            }
            await db.SaveChangesAsync();
        }

        //These are the values used. Initial Values will need to be subject to later balancing
        //Depreceated Delete?
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

        public async Task ReadGameStateConfig(GameState gameState)
        {
            string DataPath = @"./GameStateConfig.xml";
            XmlDocument Xmldata = new XmlDocument();
            Xmldata.Load(DataPath);
            XmlNode xmlNode = Xmldata.LastChild.SelectSingleNode("Nation");


            await ReadNation(gameState, Xmldata.LastChild.SelectSingleNode("Nation"), Xmldata.LastChild.SelectSingleNode("ValueTemplates"));
            await GenerateValueRelationships(Xmldata.LastChild.SelectSingleNode("ValueRelationShips"));
        }

        public async Task ReadNation(GameState gameState, XmlNode NationXml, XmlNode ValueTemplatesXml)
        {
            using var db = new IcarusContext();

            Nation NewNation = new Nation(){
                Name = NationXml.SelectSingleNode("Name").InnerText,
                Description = NationXml.SelectSingleNode("Description").InnerText
            };

            List<ValueTemplate> ValueTemplates = new List<ValueTemplate>();

            foreach(XmlNode ValueTemplate in ValueTemplatesXml)
            {
                ValueTemplates.Add(new ValueTemplate()
                {
                    Name = ValueTemplate.SelectSingleNode("Name").InnerText,
                    Description = ValueTemplate.SelectSingleNode("Description").InnerText,
                    TAG = ValueTemplate.SelectSingleNode("Tag").InnerText
                });
            }

            foreach(XmlNode province in NationXml.SelectSingleNode("Provinces").ChildNodes)
            {
                Province NewProvince = new Province()
                {
                    Name = province.SelectSingleNode("Name").InnerText,
                    Description = province.SelectSingleNode("Description").InnerText
                };
                foreach(XmlNode Value in province.SelectSingleNode("Values").ChildNodes)
                {
                    ValueTemplate vTemplate = ValueTemplates.FirstOrDefault(v => v.TAG == Value.Name);
                    NewProvince.Values.Add(new Value()
                    {
                        Name = vTemplate.Name,
                        Description = vTemplate.Description,
                        TAG = vTemplate.TAG,
                        BaseBalue = float.Parse(Value.InnerText, System.Globalization.CultureInfo.InvariantCulture),
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

                NewNation.Provinces.Add(NewProvince);
            }

            gameState.Nation= NewNation;
            await db.SaveChangesAsync();
        }

        //What we wanna do here is 
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
            }

            List<ValueRelationship> Relationships = new List<ValueRelationship>();

            foreach(Value Value in Values)
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

            db.Relationships.AddRange(Relationships);

            await db.SaveChangesAsync();
        }
    }

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
}
