using System;
using System.Linq;
using System.Threading.Tasks;
using Icarus.Context;
using Icarus.Context.Models;

namespace Icarus.Services
{
    public class AgeService
    {
        const int CHANCE_AGE_START = 60;
        const double DEATH_CHANCE = 0.5;

        private readonly DeathService _deathService;

        public AgeService(DeathService deathService)
        {
            _deathService = deathService;
        }

        public async Task CalcDeathChance(string characterId, int amountOfYears)
        {
            using var db = new IcarusContext();

            var character = db.Characters.Single(c => c.CharacterId == characterId);
                
            var gameState = db.GameStates.First();

            var charAge = gameState.Year - character.YearOfBirth;

            var yearsToCalc = charAge - CHANCE_AGE_START;

            if (amountOfYears < yearsToCalc)
            {
                yearsToCalc = amountOfYears;
            }

            var rand = new Random();

            var death = false;

            foreach(var x in Enumerable.Range(0, 5))
            {
                if(rand.NextDouble() < DEATH_CHANCE)
                {
                    death = true;
                    break;
                }
            }

            if (death)
            {
                _deathService.KillCharacter(character.DiscordUserId);
            }
        }
    }
}