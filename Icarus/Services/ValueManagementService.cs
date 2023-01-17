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
                List<ValueRelationship> Relationships = _icarusContext.Relationships.Where(vr => vr.Target == Value);
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
            //How To Best do This
            //Ideally avoid hard coding this in
            //Maybe some XML Format?
            //Maybe Hardcoding... not sure.
            await _icarusContext.SaveChangesAsync();
        }

    }
}
