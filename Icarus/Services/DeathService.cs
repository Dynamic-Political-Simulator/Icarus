using Azure;
using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Services
{
    public class DeathService
    {
        private readonly CharacterService _characterService;
        private readonly GraveyardService _graveyardService;
        private readonly DebugService _debugService;
        private readonly TickService _tickService;

        public DeathService(CharacterService characterService, GraveyardService graveyardService, DebugService debugService, TickService tickService)
        {
            _characterService = characterService;
            _graveyardService = graveyardService;
            _debugService = debugService;
            _tickService = tickService;

            _tickService.TickEvent += CheckDeathTimer;
        }

        public void CheckDeathTimer()
        {
            using var db = new IcarusContext();

            var charIdsOnTimer = db.DeathTimer.Where(c => c.TimeKilled == null).ToList();

            foreach (var charId in charIdsOnTimer)
            {
                _ = KillCharacterById(charId.CharacterId);
            }
        }

        public async Task OldAgeDeath(PlayerCharacter character)
        {
            using var db = new IcarusContext();

            var newDeathTimer = new DeathTimer()
            {
                CharacterId = character.CharacterId
            };

            db.DeathTimer.Add(newDeathTimer);
            await db.SaveChangesAsync();

            _ = _debugService.PrintToChannels($"Character {character.CharacterName} has been added to the 24-hour death timer.");
        }

        public async Task<string> KillCharacterById(string characterId)
        {
            try
            {
                var activeCharacter = await _characterService.GetCharacter(characterId);

                using var db = new IcarusContext();

                var gameState = db.GameStates.First();

                activeCharacter.YearOfDeath = gameState.Year;

                db.Update(activeCharacter);
                await db.SaveChangesAsync();

                _ = _graveyardService.SendGraveyardMessage(activeCharacter);

                return $"Press F for {activeCharacter.CharacterName}";
            }
            catch (NoActiveCharacterException)
            {
                return "Could not kill character. No active character found.";
            }
        }

        public async Task<string> KillCharacter(string discordId)
        {
            try
            {
                var activeCharacter = await _characterService.GetActiveCharacter(discordId);

                using var db = new IcarusContext();

                var gameState = db.GameStates.First();

                activeCharacter.YearOfDeath = gameState.Year;

                db.Update(activeCharacter);
                await db.SaveChangesAsync();

                _ = _graveyardService.SendGraveyardMessage(activeCharacter);

                return $"Press F for {activeCharacter.CharacterName}";
            }
            catch (NoActiveCharacterException)
            {
                return "Could not kill character. No active character found.";
            }
        }
    }
}
