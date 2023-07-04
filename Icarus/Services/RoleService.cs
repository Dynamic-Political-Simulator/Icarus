using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
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
            try
            {
                var guild = _client.GetGuild(guildId);

                var guildUser = guild.GetUser(discordId);

                if (guildUser == null)
                {
                    _ = _debugService.PrintToChannels($"Could not find user with id {discordId}, they likely left the server.");
                    return;
                }

                var roleIds = guildUser.Roles.Select(r => r.Id);

                var roleIdsToRemove = roleIdsToCheck.Where(r => roleIds.Contains(r));

                // I hate to do this but Discord's rate limit leaves me no choice
                foreach (var roleIdToRemove in roleIdsToRemove)
                {
                    await guildUser.RemoveRoleAsync(roleIdToRemove);
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                _ = _debugService.PrintToChannels($"Exception occured in RemoveRoles: {ex.Message}");
            }
        }

        public async Task AddRole(ulong roleId, ulong discordId, ulong guildId)
        {
            try
            {
                var guild = _client.GetGuild(guildId);

                var guildUser = guild.GetUser(discordId);

                if (guildUser == null)
                {
                    _ = _debugService.PrintToChannels($"Could not find user with id {discordId}, they likely left the server.");
                    return;
                }

                var roleIds = guildUser.Roles.Select(r => r.Id);

                if (!roleIds.Contains(roleId))
                {
                    var role = guild.GetRole(roleId);

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
