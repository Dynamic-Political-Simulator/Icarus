using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Icarus.Context;
using Icarus.Context.Models;

namespace Icarus.Services
{
    public class AgeService
    {
        const int CHANCE_AGE_START = 60;
        const double DEATH_CHANCE = 0.05;

        const int YEARS_PER_DAY = 1;

        private readonly DeathService _deathService;
        private readonly DebugService _debugService;
        private readonly TickService _tickService;

        public AgeService(DeathService deathService, DebugService debugService, TickService tickService)
        {
            _deathService = deathService;
            _debugService = debugService;
            _tickService = tickService;

            _tickService.TickEvent += DoAging;
        }

        public async void DoAging()
        {
            using var db = new IcarusContext();

            var gameState = db.GameStates.First();

            var tickToday = gameState.LastAgingEvent.Date == DateTime.UtcNow.Date;

            if (gameState.AgingEnabled && !tickToday)
            {
                _ = _debugService.PrintToChannels("Fired aging event.");
                var allLivingCharacters = db.Characters.Where(c => c.YearOfDeath == -1);

                foreach (var character in allLivingCharacters)
                {
                    var onDeathTimer = db.DeathTimer.SingleOrDefault(dt => dt.CharacterId == character.CharacterId);

                    // ADJUST YEARS PER DAY HERE
                    // Only fire if the character doesn't already have a death timer
                    if (onDeathTimer == null) _ = CalcDeathChance(character, YEARS_PER_DAY);
                }

                gameState.LastAgingEvent = DateTime.UtcNow;
                gameState.Year += YEARS_PER_DAY;
                await db.SaveChangesAsync();

                _ = _debugService.PrintToChannels($"Aging event done, the year is now {gameState.Year}.");
            }
            else if (!gameState.AgingEnabled && !tickToday)
            {
                _ = _debugService.PrintToChannels("Tried to fire aging event, but aging was disabled.");
            }
        }

        public async Task<bool> ToggleAging()
        {
            using var db = new IcarusContext();

            var gameState = db.GameStates.FirstOrDefault();

            gameState.AgingEnabled = !gameState.AgingEnabled;

            db.SaveChanges();

            _ = _debugService.PrintToChannels($"AgingEnabled was to {gameState.AgingEnabled}.");

            return gameState.AgingEnabled;
        }

        public async Task CalcDeathChance(PlayerCharacter character, int amountOfYears)
        {
            using var db = new IcarusContext();
                
            var gameState = db.GameStates.First();

            var charAge = gameState.Year - character.YearOfBirth;

            // Return if the character is not old enough for death chance yet
            if (charAge < CHANCE_AGE_START)
            {
                return;
            }

            var yearsToCalc = charAge - CHANCE_AGE_START;

            if (amountOfYears < yearsToCalc)
            {
                yearsToCalc = amountOfYears;
            }

            var rand = new Random();

            var death = false;

            foreach(var x in Enumerable.Range(0, yearsToCalc))
            {
                if(rand.NextDouble() < DEATH_CHANCE)
                {
                    death = true;
                    break;
                }
            }

            if (death)
            {
                _ = _deathService.OldAgeDeath(character);
            }
        }
    }
}