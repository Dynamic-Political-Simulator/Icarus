using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Services
{
    public class PurseService
    {
        private readonly CharacterService _characterService;

        private readonly DebugService _debugService;

        public PurseService(CharacterService characterService, DebugService debugService)
        {
            _characterService = characterService;
            _debugService = debugService;
        }

        public async Task<List<CharacterCurrencyDto>> GetPurse(ulong discordId)
        {
            using var db = new IcarusContext();

            var character = await _characterService.GetActiveCharacter(discordId.ToString());

            var moneys = db.CharacterCurrency.Include(cc => cc.Currency).Where(cc => cc.PlayerCharacterId == character.CharacterId);

            var returnList = new List<CharacterCurrencyDto>();

            foreach (var money in moneys)
            {
                returnList.Add(new CharacterCurrencyDto { CurrencyName = money.Currency.Name, CurrencyAmount = money.Amount });
            }

            return returnList;
        }

        public async Task<List<Currency>> GetCurrencies()
        {
            using var db = new IcarusContext();

            return await db.Currencies.ToListAsync();
        }

        public async Task GiveMoney(ulong discordId, string )
    }
}
