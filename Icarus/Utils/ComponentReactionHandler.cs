﻿using Discord.WebSocket;
using Icarus.Context.Models;
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
        private readonly DebugService _debugService;

        public ComponentReactionHandlerService(DiscordSocketClient client, CharacterService characterService, DebugService debugService)
        {
            _client = client;

            _characterService = characterService;
            _debugService = debugService;

            _client.SelectMenuExecuted += HandleSelectMenu;
        }

        public async Task HandleSelectMenu(SocketMessageComponent arg)
        {
            _ = _debugService.PrintToChannels($"Received Select Menu Event with custom id {arg.Data.CustomId}.");

            switch (arg.Data.CustomId)
            {
                case "char-goi-selection":
                    await _characterService.HandleGroupOfInterestSelectMenu(arg);
                    break;
            }
        }
    }
}
