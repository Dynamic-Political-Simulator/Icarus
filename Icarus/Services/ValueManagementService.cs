using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Icarus.Context;
using Icarus.Context.Models;

namespace Icarus.Services
{
    public class ValueManagementService
    {
        private readonly IcarusContext _icarusContext;

        public ValueManagementService(IcarusContext context)
        {
            _icarusContext = context;
        }

        //This will need to be executed every Tick
        public async void ValueTick()
        {
            await UpdateValues();
            await ReEvaluteModifiers();

        }
        public async Task UpdateValues()
        {
            foreach (Value Value in _icarusContext.Values)
            {
                CalculateNewValue(Value);
            }
            await _icarusContext.SaveChangesAsync();
        }

        public float GetValueChange(Value value)
        {
            return AggregateModifiers(value) + value.RelationInducedChange;
        }

        public float CalculateNewValue(Value Value)
        {
            float TotalModifier = AggregateModifiers(Value);
            Value._Value += TotalModifier;
            return Value._Value;
        }

        public float AggregateModifiers(Value Value)
        {
            float Total = 0f;
            ValueModifier vm;
            //Aggregate the local Modifiers
            foreach (Modifier modifier in Value.Province.Modifiers.Where(m => m.Modifiers.FirstOrDefault(vm => vm.ValueName == Value.Name) != null)) 
            {
                vm = modifier.Modifiers.FirstOrDefault(vm => vm.ValueName == Value.Name);
                Total += vm.Modifier;
            }
            //Aggregate the global Modifiers
            foreach (Modifier modifier in Value.Province.Nation.Modifiers.Where(m => m.Modifiers.FirstOrDefault(vm => vm.ValueName == Value.Name) != null))
            {
                vm = modifier.Modifiers.FirstOrDefault(vm => vm.ValueName == Value.Name);
                Total += vm.Modifier;
            }

            return Total;
        }

        public async Task ReEvaluateRelations()
        {
            foreach (Value Value in _icarusContext.Values)
            {
                Value.RelationInducedChange = 0;
                List<ValueRelationship> Relationships = _icarusContext.Relationships.AsQueryable().Where(vr => vr.Target == Value).ToList();
                foreach(ValueRelationship relationship in Relationships)
                {
                    float change = Math.Clamp(relationship.Factor * relationship.Origin._Value, relationship.Min, relationship.Max);
                    Value.RelationInducedChange += change;
                }

            }
            await _icarusContext.SaveChangesAsync();
        }



        public async Task ReEvaluteModifiers()
        {
            foreach (Modifier modifier in _icarusContext.Modifiers)
            {
                switch (modifier.Type)
                {
                    case ModifierType.Temporary:
                        modifier.Duration -= 1;
                        if (modifier.Duration == 0)
                        {
                            _icarusContext.Modifiers.Remove(modifier);
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
                                    _icarusContext.ValueModifiers.Remove(vm);
                                }
                            }
                            else
                            {
                                vm.Modifier -= vm.Decay;
                                if (vm.Modifier < 0)
                                {
                                    modifier.Modifiers.Remove(vm);
                                    _icarusContext.ValueModifiers.Remove(vm);
                                }
                            }
                        }
                        if (modifier.Modifiers.Count == 0)
                        {
                            _icarusContext.Modifiers.Remove(modifier);
                        }
                        break;
                }
            }
            await _icarusContext.SaveChangesAsync();
        }

        //These are the values used. Initial Values will need to be subject to later balancing
        //Maybe have some better way than them being hardcoded?
        public List<Value> GenerateValueTemplate()
        {
            List<Value> Values = new List<Value>();
            Value StandardOfLiving = new Value() {
                Name= "StandardOfLiving",
                _Value = 10,
            };
            Values.Add(StandardOfLiving);
            Value Literacy = new Value()
            {
                Name = "Literacy",
                _Value = 10,
            };
            Values.Add(Literacy);
            Value Security = new Value()
            {
                Name = "Security",
                _Value = 10,
            };
            Values.Add(Security);
            Value Health = new Value()
            {
                Name = "Health",
                _Value = 10,
            };
            Values.Add(Health);
            Value Housing = new Value()
            {
                Name = "Housing",
                _Value = 10,
            };
            Values.Add(Housing);
            Value Urbanization = new Value()
            {
                Name = "Urbanization",
                _Value = 10,
            };
            Values.Add(Urbanization);
            Value EconomicStrength = new Value()
            {
                Name = "Economic Strength",
                _Value = 10,
            };
            Values.Add(EconomicStrength);
            Value Mechanisation = new Value()
            {
                Name = "Mechanisation",
                _Value = 10,
            };
            Values.Add(Mechanisation);
            Value IndustrialOutput = new Value()
            {
                Name = "IndustrialOutput",
                _Value = 10,
            };
            Values.Add(Mechanisation);
            Value Infrastructure = new Value()
            {
                Name = "Infrastructure",
                _Value = 10,
            };
            Values.Add(Infrastructure);
            Value Destitution = new Value()
            {
                Name = "Destitution",
                _Value = 10,
            };
            Values.Add(Destitution);


            return Values;
        }

        //What we wanna do here is 
        public async Task GenerateValueRelationships(List<Value> Values)
        {
            string DataPath = "D:\\SeasonDPS\\Icarus\\Icarus\\Context\\ValueRelationShips.xml";
            XmlDocument Xmldata = new XmlDocument();
            Xmldata.Load(DataPath);
            List<RelationShipDTO> RelationShipDTOs = new List<RelationShipDTO>();
            foreach (XmlNode relationship in Xmldata.FirstChild.ChildNodes)
            {
                RelationShipDTOs.Add(new RelationShipDTO()
                {
                    Origin = relationship.SelectSingleNode("/Origin").InnerText,
                    Target = relationship.SelectSingleNode("/Target").InnerText,
                    Factor = float.Parse(relationship.SelectSingleNode("/Factor").InnerText),
                    Max = float.Parse(relationship.SelectSingleNode("/Max").InnerText),
                    Min = float.Parse(relationship.SelectSingleNode("/Min").InnerText)
                });
            }

            List<ValueRelationship> Relationships = new List<ValueRelationship>();

            foreach(Value Value in Values)
            {
                List<RelationShipDTO> ValidDTOs = RelationShipDTOs.Where(v=> v.Origin == Value.Name).ToList();
                foreach (RelationShipDTO DTO in ValidDTOs)
                {
                    Value Target = Values.FirstOrDefault(v => v.Name == DTO.Target);
                    if (Target != null)
                    {
                        Relationships.Add(new ValueRelationship()
                        {
                            Origin = Value,
                            Target = Target,
                            Factor = DTO.Factor,
                            Max = DTO.Max,
                            Min = DTO.Min
                        });
                    }
                    else
                    {
                        //Crash
                        Console.WriteLine("SOMETHING HAS GONE WRONG. THERE WAS A RELATIONSHIP FOUND WITH AN EXISTING ORIGIN VALUE BUT A MISSING TARGET VALUE");
                    }
                }
            }

            _icarusContext.Relationships.AddRange(Relationships);

            await _icarusContext.SaveChangesAsync();
        }


    }

    public class RelationShipDTO
    {
        public string Origin { get; set; }
        public string Target { get; set; }
        public float Factor { get; set; }
        public float Max { get; set; }
        public float Min { get; set; }
    }
}
