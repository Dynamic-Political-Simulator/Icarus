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

        public async Task UpdateCharacterBio(string discordId, string bio)
        {
            using var db = new IcarusContext();

            var active = await GetActiveCharacter(discordId);

            active.CharacterDescription = bio;
        }
    }
}
