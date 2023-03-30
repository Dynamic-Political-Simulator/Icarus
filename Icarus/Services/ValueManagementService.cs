using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Icarus.Context;
using Icarus.Context.Models;

namespace Icarus.Services
{
    public class ValueManagementService
    {
        private readonly IcarusContext _icarusContext;
        public List<Value> Values;
        public List<ValueModifier> Modifiers;

        public ValueManagementService(IcarusContext context)
        {
            _icarusContext = context;
            Values = _icarusContext.Values.ToList();
            Modifiers = _icarusContext.Modifiers.ToList();

        }

        //This will need to be executed every Tick
        public async void ValueTick()
        {
            await UpdateValues();
            await ReEvaluteModifiers();

        }
        public async Task UpdateValues()
        {
            foreach (Value Value in Values)
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
            foreach (ValueModifier Modifier in Value.Modifiers)
            {
                Total += Modifier.Modifier;
            }

            return Total;
        }

        public async Task ReEvaluateRelations()
        {
            foreach (Value Value in Values)
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
            foreach (ValueModifier modifier in Modifiers)
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
                        if (modifier.Modifier > 0)
                        {
                            modifier.Modifier -= modifier.Decay;
                            if (modifier.Modifier < 0)
                            {
                                _icarusContext.Modifiers.Remove(modifier);
                            }
                        }
                        else
                        {
                            modifier.Modifier += modifier.Decay;
                            if (modifier.Modifier > 0)
                            {
                                _icarusContext.Modifiers.Remove(modifier);
                            }
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
            Values = new List<Value>();
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
        public List<ValueRelationship> GenerateValueRelationships(List<Value> Values)
        {
            return null;
        }

    }
}
