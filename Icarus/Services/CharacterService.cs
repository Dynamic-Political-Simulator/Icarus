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
        public async Task DisableRoleSync(ulong discordId)
        {
            var character = await GetActiveCharacter(discordId.ToString());

            character.GoiRoleSync = false;

            using var db = new IcarusContext();

            db.Characters.Update(character);
            await db.SaveChangesAsync();
        }

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

		public async Task<int> GetCharacterAge(string characterId)
		{
			PlayerCharacter character = await GetCharacter(characterId);

			if (character.YearOfDeath > 0) {
				return character.YearOfDeath - character.YearOfBirth;
			} else {
				using var db = new IcarusContext();

				GameState state = db.GameStates.FirstOrDefault();
				if (state == null) return -1; // Something went awry
				return state.Year - character.YearOfBirth;
			}
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

        public async Task UpdateCharacterName(string discordId, string name)
        {
            if (name.Length > 64)
            {
                throw new ArgumentException();
            }

            using var db = new IcarusContext();

            var active = await GetActiveCharacter(discordId);

            active.CharacterName = name;

            db.Update(active);
            await db.SaveChangesAsync();
        }

        public async Task UpdateCharacterBio(string discordId, string bio)
        {
            if (bio.Length > 1000)
            {
                throw new ArgumentException();
            }

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

            var allChars = await db.Characters.Include(c => c.GroupOfInterest).Where(c => c.YearOfDeath == -1).ToListAsync();

            return allChars;
        }

        public async Task HandleGroupOfInterestSelectMenu(SocketMessageComponent arg)
        {
            var selection = arg.Data.Values.FirstOrDefault();

            var character = await GetActiveCharacter(arg.User.Id.ToString());

            using var db = new IcarusContext();

            var newGoi = db.GroupOfInterests.Single(g => g.Id == int.Parse(selection));

            character.GoIid = newGoi.Id;
            character.GroupOfInterest = newGoi;

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
