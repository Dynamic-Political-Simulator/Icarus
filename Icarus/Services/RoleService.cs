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

        public void SetRole(ulong discordId, ulong roleId, ulong guildId)
        {
            var guild = _client.GetGuild(guildId);

            var guildUser = guild.GetUser(discordId);

            guildUser.AddRoleAsync(roleId);
        }
    }
}
