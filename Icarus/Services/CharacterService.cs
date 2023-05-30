using Discord.API;
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
    public class CharacterService
    {
        public async Task<PlayerCharacter> GetActiveCharacter(string discordId)
        {
            using var db = new IcarusContext();

            var active = await db.Characters.SingleOrDefaultAsync(c => c.DiscordUserId == discordId && c.YearOfDeath == -1);

            if (active == null)
            {
                throw new NoActiveCharacterException(discordId);
            }

            return active;
        }

        public async Task CreateNewCharacter(string discordId, string characterName, int startingAge)
        {
            using var db = new IcarusContext();

            var active = await GetActiveCharacter(discordId);

            if (active != null)
            {
                throw new ExistingActiveCharacterException(discordId);
            }

            if (characterName.Length > 64)
            {
                throw new ArgumentException();
            }

            var rand = new Random();

            var gameState = db.GameStates.First();

            var newChar = new PlayerCharacter()
            {
                DiscordUserId= discordId,
                CharacterName= characterName,
                YearOfBirth = (int)(gameState.Year - (startingAge + rand.NextInt64(-2, 3)))
            };

            db.Characters.Add(newChar);

            await db.SaveChangesAsync();
        }

        public async Task UpdateCharacterBio(string discordId, string bio)
        {
            using var db = new IcarusContext();

            var active = await GetActiveCharacter(discordId);

            active.CharacterDescription = bio;

            await db.SaveChangesAsync();
        }

        public async Task UpdateCharacterCareer(string discordId, string career)
        {
            if (career.Length > 64)
            {
                throw new ArgumentException();
            }

            using var db = new IcarusContext();

            var active = await GetActiveCharacter(discordId);

            active.Career = career;

            await db.SaveChangesAsync();
        }
    }
}
