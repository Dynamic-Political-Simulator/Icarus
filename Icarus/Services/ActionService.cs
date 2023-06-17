using Discord.WebSocket;
using Icarus.Context;
using Icarus.Context.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Services
{
    public class ActionService
    {
        public string ExampleAction(PlayerCharacter character)
        {
            return "Example aciton.";
        }

        public async Task<string> GiveToken(SocketGuildUser Target, string tokenType, int amount, string userId)
        {
            if (amount > 7)
            {
                return "May not give a character more than seven favours of any type.";
            }

            using var db = new IcarusContext();

            var tokenTypeExists = db.TokenTypes.FirstOrDefault(tt => tt.TokenTypeName.ToLowerInvariant() == tokenType.ToLowerInvariant());

            if (tokenTypeExists == null)
            {
                return $"Could not find token of type {tokenType}.";
            }

            var character = db.Characters.Include(c => c.Tokens)
                .FirstOrDefault(c => c.DiscordUserId == userId && c.YearOfDeath != -1);

            if (character == null)
            {
                return $"Target user does not have an active character.";
            }

            var tokenEntry = character.Tokens.FirstOrDefault(t => t.TokenTypeId == tokenTypeExists.TokenTypeName);

            if (tokenEntry == null)
            {
                var newTokenEntry = new CharacterToken()
                {
                    PlayerCharacterId = character.CharacterId,
                    TokenTypeId = tokenTypeExists.TokenTypeName,
                    Amount = amount
                };

                character.Tokens.Add(newTokenEntry);

                tokenEntry = newTokenEntry;
            }

            if (tokenEntry.Amount > 7) tokenEntry.Amount = 7;

            db.Update(character);
            await db.SaveChangesAsync();

            return "Added favours.";
        }

        public List<string> GetTokenTypes()
        {
            using var db = new IcarusContext();

            return db.TokenTypes.ToList().Select(tt => tt.TokenTypeName).ToList();
        } 
    }
}
