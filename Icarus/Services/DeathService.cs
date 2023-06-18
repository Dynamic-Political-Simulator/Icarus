using Azure;
using Icarus.Context;
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

        public DeathService(CharacterService characterService, GraveyardService graveyardService)
        {
            _characterService = characterService;
            _graveyardService = graveyardService;
        }

        public async Task<string> KillCharacter(string discordId)
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
    }
}
