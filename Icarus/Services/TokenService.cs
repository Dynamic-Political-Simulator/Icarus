using Discord;
using Icarus.Context;
using Icarus.Context.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Services
{
    public class TokenService
    {
        private readonly IcarusContext _dbcontext;

        public TokenService(IcarusContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        public async void AddTokensToCharacter(PlayerCharacter character, ActionTokenType tokenType, int amount)
        {
            var tokenEntry = character.Tokens.SingleOrDefault(t => t.TokenType == tokenType);

            if (tokenEntry == null)
            {
                var newTokenEntry = new CharacterToken()
                {
                    PlayerCharacterId = character.CharacterId,
                    TokenType = tokenType,
                    Amount = 0
                };

                character.Tokens.Add(newTokenEntry);

                tokenEntry = newTokenEntry;
            }

            tokenEntry.Amount += amount;

            _dbcontext.Update(character);
            await _dbcontext.SaveChangesAsync();
        }

        public async Task<int> RemoveTokensFromCharacter(PlayerCharacter character, ActionTokenType tokenType, int amount)
        {
            var tokenEntry = character.Tokens.SingleOrDefault(t => t.TokenType == tokenType);

            if (tokenEntry == null)
            {
                var newTokenEntry = new CharacterToken()
                {
                    PlayerCharacterId = character.CharacterId,
                    TokenType = tokenType,
                    Amount = 0
                };

                character.Tokens.Add(newTokenEntry);

                tokenEntry = newTokenEntry;

                _dbcontext.Update(character);

                await _dbcontext.SaveChangesAsync();
            }

            var amountInitial = tokenEntry.Amount;

            tokenEntry.Amount -= amount;

            if (tokenEntry.Amount < 0) tokenEntry.Amount = 0;

            _dbcontext.Update(character);

            await _dbcontext.SaveChangesAsync();

            var result = tokenEntry.Amount < amount ? amountInitial : -1;

            return result;
        }
    }
}
