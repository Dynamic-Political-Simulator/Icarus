﻿using Discord.WebSocket;
using Icarus.Context;
using Icarus.Context.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Services
{
    public class DebugService
    {
        private readonly DiscordSocketClient _client;

        private readonly TickService _tickService;

        public DebugService(DiscordSocketClient client, TickService tickService)
        { 
            _client = client;
            _tickService = tickService;

            _tickService.TickEvent += PrintTick;
        }

        public async Task AddChannel(ulong channelId)
        {
            var newChannel = new DebugChannel() { ChannelId = channelId };

            using var db = new IcarusContext();

            db.DebugChannels.Add(newChannel);
            await db.SaveChangesAsync();
        }

        public void PrintTick()
        {
            _ = PrintToChannels("Tick occured.");
        }

        public async Task PrintToChannels(string message)
        {
            using var db = new IcarusContext();

            var channels = await db.DebugChannels.ToListAsync();

            var errorList = new List<DebugChannel>();

            foreach (var channel in channels)
            {
                try
                {
                    var discordTextChannel = (SocketTextChannel)await _client.GetChannelAsync(channel.ChannelId);
                    _ = discordTextChannel.SendMessageAsync(message);
                }
                catch
                {
                    errorList.Add(channel);
                }
            }

            db.DebugChannels.RemoveRange(errorList);
            db.SaveChangesAsync();
        }
    }
}