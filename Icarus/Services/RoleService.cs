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

            var roleIdsToRemove = roleIdsToCheck.Where(r => roleIds.Contains(r));

            await guildUser.RemoveRolesAsync(roleIdsToCheck);
        }

        public async Task AddRole(ulong roleId, ulong discordId, ulong guildId)
        {
            try
            {
                var guild = _client.GetGuild(guildId);

                _ = _debugService.PrintToChannels($"1");

                var guildUser = guild.GetUser(discordId);

                _ = _debugService.PrintToChannels($"2");

                var roleIds = guildUser.Roles.Select(r => r.Id);

                _ = _debugService.PrintToChannels($"3");

                if (!roleIds.Contains(roleId))
                {
                    _ = _debugService.PrintToChannels($"4");

                    var role = guild.GetRole(roleId);

                    _ = _debugService.PrintToChannels($"5");

                    await guildUser.AddRoleAsync(role);

                    _ = _debugService.PrintToChannels($"Gave role {role.Name} to user {guildUser.Username}");
                }
            }
            catch(Exception ex)
            {
                _ = _debugService.PrintToChannels($"Exception occured in AddRole: {ex.Message}");
            }
            
        }
    }
}
