using Discord.WebSocket;
using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Dto;
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
        private readonly CharacterService _characterService;

        public ActionService(CharacterService characterService)
        {
            _characterService = characterService;
        }

        public string ExampleAction(PlayerCharacter character)
        {
            return "Example aciton.";
        }

        public async Task<string> GiveToken(SocketGuildUser Target, string tokenType, int amount)
        {
            if (amount > 7)
            {
                return "May not give a character more than seven favours of any type.";
            }

            using var db = new IcarusContext();

            var tokenTypeExists = db.TokenTypes.FirstOrDefault(tt => tt.TokenTypeName.ToLower() == tokenType.ToLower());

            if (tokenTypeExists == null)
            {
                return $"Could not find favour of type {tokenType}.";
            }

            var character = db.Characters.Include(c => c.Tokens).ThenInclude(t => t.TokenType)
                .FirstOrDefault(c => c.DiscordUserId == Target.Id.ToString() && c.YearOfDeath == -1);

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
                    Amount = 0
                };

                character.Tokens.Add(newTokenEntry);

                tokenEntry = newTokenEntry;
            }

            tokenEntry.Amount += amount;

            if (tokenEntry.Amount > 7) tokenEntry.Amount = 7;

            db.Update(character);
            await db.SaveChangesAsync();

            return "Added favours.";
        }

        public async Task<List<CharacterToken>> GetAllTokensForProfileActiveCharacter(string discordId)
        {
            using var db = new IcarusContext();

            var activeCharacter = await _characterService.GetActiveCharacter(discordId);

            var tokens = await db.Tokens.Include(t => t.TokenType).Where(t => t.PlayerCharacterId == activeCharacter.CharacterId).ToListAsync();

            return tokens;
        }

        public List<string> GetTokenTypes()
        {
            using var db = new IcarusContext();

            return db.TokenTypes.ToList().Select(tt => tt.TokenTypeName).ToList();
        } 

        public async Task<string> NewTokenType(string name)
        {
            using var db = new IcarusContext();

            var allTypes = db.TokenTypes.ToList();

            var existing = allTypes.FirstOrDefault(at => at.TokenTypeName.ToLowerInvariant() == name.ToLowerInvariant());

            if (existing != null)
            {
                return "Could not create favour type as it already exists.";
            }

            var newTokenType = new CharacterTokenType()
            {
                TokenTypeName = name
            };

            db.TokenTypes.Add(newTokenType);

            await db.SaveChangesAsync();

            return $"Added favour type with name {name}";
        }

        public async IAsyncEnumerable<CharacterWithDebtDto> GetLivingCharactersWithDebt()
        {
            using var db = new IcarusContext();

            var characters = await _characterService.GetAllCharactersIncludeGoI();

            foreach (var character in characters)
            {
                var favours = db.Tokens.Include(t => t.TokenType)
                    .Where(t => t.PlayerCharacterId == character.CharacterId && t.Amount < 0).ToList();

                if (!favours.Any())
                {
                    continue;
                }

                var newDto = new CharacterWithDebtDto()
                {
                    CharacterName = character.CharacterName,
                    DiscordId = character.DiscordUserId,
                    FavourDebtLines = new List<FavourDebtLine>()
                };

                foreach (var favour in favours)
                {
                    newDto.FavourDebtLines.Add(new FavourDebtLine { FavourName = favour.TokenType.TokenTypeName, Amount = favour.Amount });
                }

                yield return newDto;
            }
        }
    }
}
