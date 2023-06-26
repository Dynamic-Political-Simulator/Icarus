using Discord.API;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Icarus.Services
{
    public class CharacterService
    {
        public async Task<PlayerCharacter> GetCharacter(string characterId)
        {
            using var db = new IcarusContext();

            return db.Characters.SingleOrDefault(c => c.CharacterId == characterId);
        }

        public async Task<PlayerCharacter> GetActiveCharacter(string discordId)
        {
            using var db = new IcarusContext();

            var active = await db.Characters.Include(c => c.GroupOfInterest).SingleOrDefaultAsync(c => c.DiscordUserId == discordId && c.YearOfDeath == -1);

            if (active == null)
            {
                throw new NoActiveCharacterException(discordId);
            }

            return active;
        }

        public async Task CreateNewCharacter(string discordId, string characterName, int startingAge)
        {
            using var db = new IcarusContext();

            var active = await db.Characters.SingleOrDefaultAsync(c => c.DiscordUserId == discordId && c.YearOfDeath == -1);

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
                DiscordUserId = discordId,
                CharacterName = characterName,
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

            db.Update(active);
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

            db.Update(active);
            await db.SaveChangesAsync();
        }

        public async Task UpdateCharacterCulture(string discordId, string culture)
        {
            if (culture.Length > 64)
            {
                throw new ArgumentException();
            }

            using var db = new IcarusContext();

            var active = await GetActiveCharacter(discordId);

            active.Culture = culture;

            db.Update(active);
            await db.SaveChangesAsync();
        }

        public async Task UpdateCharacterPG(string discordId, string pg)
        {
            if (pg.Length > 64)
            {
                throw new ArgumentException();
            }

            using var db = new IcarusContext();

            var active = await GetActiveCharacter(discordId);

            active.PrivilegedGroup = pg;

            db.Update(active);
            await db.SaveChangesAsync();
        }

        public async Task<List<PlayerCharacter>> GetAllCharactersIncludeGoI()
        {
            using var db = new IcarusContext();

            var allChars = await db.Characters.Include(c => c.GroupOfInterest).ToListAsync();

            return allChars;
        }

        public async Task HandleGroupOfInterestSelectMenu(SocketMessageComponent arg)
        {
            var selection = arg.Data.Values.FirstOrDefault();

            var character = await GetActiveCharacter(arg.User.Id.ToString());

            using var db = new IcarusContext();

            character.GoIid = int.Parse(selection);

            db.Update(character);
            await db.SaveChangesAsync();

            await arg.RespondAsync("Group of Interest set.", ephemeral: true);
        }

        public async Task UpdateGoI(string characterId, int goiId)
        {
            using var db = new IcarusContext();

            var character = db.Characters.Single(c => c.CharacterId == characterId);

            character.GoIid = goiId;

            db.Update(character);
            await db.SaveChangesAsync();
        }
    }
}
