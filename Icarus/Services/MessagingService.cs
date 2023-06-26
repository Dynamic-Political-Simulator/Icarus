using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Icarus.Services
{
    public class MessagingService
    {
        private readonly DebugService _debugService;

        private readonly DiscordSocketClient _client;

        public MessagingService(DebugService debugService, DiscordSocketClient client)
        {
            _debugService = debugService;
            _client = client;
        }

        public async Task SendMessageToUser(ulong discordId, string message)
        {
            var discordUser = await _client.GetUserAsync(discordId);

            try
            {
                _ = discordUser.SendMessageAsync(message);
            }
            catch
            {
                _ = _debugService.PrintToChannels($"Attempted to send 24h warning to {discordUser.Username} but failed.");
            }
        }
    }
}
