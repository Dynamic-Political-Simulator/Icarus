using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Services
{
    public class RoleService
    {
        private readonly DiscordSocketClient _client;

        public RoleService(DiscordSocketClient client)
        {
            _client = client;
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
            var guild = _client.GetGuild(guildId);

            var guildUser = guild.GetUser(discordId);

            var roleIds = guildUser.Roles.Select(r => r.Id);

            if (!roleIds.Contains(roleId))
            {
                await guildUser.AddRoleAsync(roleId);
            }
        }
    }
}
