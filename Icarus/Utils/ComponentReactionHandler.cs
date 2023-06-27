using Discord.WebSocket;
using Icarus.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Utils
{
    public class ComponentReactionHandlerService
    {
        private readonly DiscordSocketClient _client;

        private readonly CharacterService _characterService;

        public ComponentReactionHandlerService(DiscordSocketClient client, CharacterService characterService)
        {
            _client = client;

            _characterService = characterService;

            _client.SelectMenuExecuted += HandleSelectMenu;
        }

        public async Task HandleSelectMenu(SocketMessageComponent arg)
        {
            switch (arg.Data.CustomId)
            {
                case "char-goi-selection":
                    await _characterService.HandleGroupOfInterestSelectMenu(arg);
                    break;
            }
        }
    }
}
