using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Services
{
    public class RoleService
    {
        private readonly DiscordSocketClient _client;

        private readonly DebugService _debugService;

        public RoleService(DiscordSocketClient client, DebugService debugService)
        {
            _client = client;
            _debugService = debugService;
        }

        public async Task RemoveRoles(List<ulong> roleIdsToCheck, ulong discordId, ulong guildId)
        {
            var guild = _client.GetGuild(guildId);

            var guildUser = guild.GetUser(discordId);

            var roleIds = guildUser.Roles.Select(r => r.Id);

            var roleIdsToRemove = roleIdsToCheck.Select(r => !roleIds.Contains(r));

            await guildUser.RemoveRolesAsync(roleIdsToCheck);
        }

        public async Task AddRole(ulong discordId, ulong roleId, ulong guildId)
        {
            _ = _debugService.PrintToChannels($"AddRole triggered with args {discordId} {roleId} {guildId}");

            var guild = _client.GetGuild(guildId);

            var guildUser = guild.GetUser(discordId);

            var roleIds = guildUser.Roles.Select(r => r.Id);

            _ = _debugService.PrintToChannels($"RoleIds are: {roleIds.ToString()}");

            if (!roleIds.Contains(roleId))
            {
                var role = guild.GetRole(roleId);

                await guildUser.AddRoleAsync(role);

                _ = _debugService.PrintToChannels($"Gave role {role.Name} to user {guildUser.Username}");
            }
        }
    }
}
