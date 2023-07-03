using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Exceptions;
using Icarus.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Services
{
    public class GoIService
    {
        private readonly CharacterService _characterService;
        private readonly RoleService _roleService;
        private readonly TickService _tickService;

        public GoIService(CharacterService characterService, RoleService roleService, TickService tickService)
        {
            _characterService = characterService;
            _roleService = roleService;
            _tickService = tickService;

            _tickService.TickCheckEvent += SyncAllGoiRoles;
        }

        public async Task LinkGoiRole(int goiId, ulong roleId)
        {
            using var db = new IcarusContext();

            var goi = db.GroupOfInterests.SingleOrDefault(g => g.Id == goiId);

            if (goi == null) 
            {
                throw new GroupOfInterestNotFoundException();
            }

            goi.DiscordRoleId = roleId;

            db.Update(goi);
            await db.SaveChangesAsync();
        }

        public async Task<string> AddGoI(string name)
        {
            using var db = new IcarusContext();

            var alreadyExists = db.GroupOfInterests.SingleOrDefault(g => g.Name.ToLower() == name.ToLower());

            if (alreadyExists != null)
            {
                return "GoI already exists.";
            }

            var newGoi = new GroupOfInterest { Name = name };

            db.GroupOfInterests.Add(newGoi);
            await db.SaveChangesAsync();

            return $"New GoI added with name {name}.";
        }

        public async Task<List<GroupOfInterest>> GetCharacterGoIList()
        {
            using var db = new IcarusContext();

            var gois = await db.GroupOfInterests.Include(goi => goi.Characters).ToListAsync();

            return gois;
        }

        public async Task<List<GroupOfInterest>> GetAllGroups()
        {
            using var db = new IcarusContext();

            return await db.GroupOfInterests.ToListAsync();
        }

        public async void SyncAllGoiRoles()
        {
            var settings = ConfigFactory.GetConfig();

            var allLivingCharacters = await _characterService.GetAllCharactersIncludeGoI();

            foreach (var character in allLivingCharacters)
            {
                _ = SyncGoiRoles(ulong.Parse(character.DiscordUserId), settings.GuildId);
            }
        }

        public async Task SyncGoiRoles(ulong discordId, ulong guildId)
        {
            using var db = new IcarusContext();

            var character = await _characterService.GetActiveCharacter(discordId.ToString());

            var desiredRoleId = character.GroupOfInterest.DiscordRoleId;

            if (desiredRoleId == null) 
            {
                return;
            }

            var allGroups = await GetAllGroups();

            var unwantedRoleIds = allGroups.Where(ag => ag.DiscordRoleId != null).Select(ag => (ulong) ag.DiscordRoleId).ToList();
            unwantedRoleIds.Remove((ulong)desiredRoleId);

            await _roleService.AddRole((ulong)desiredRoleId, discordId, guildId);

            await _roleService.RemoveRoles(unwantedRoleIds, discordId, guildId);
        }
    }
}
