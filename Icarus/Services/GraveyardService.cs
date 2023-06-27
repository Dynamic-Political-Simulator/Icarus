using Discord.WebSocket;
using Icarus.Context;
using Icarus.Context.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Services
{
    public class GraveyardService
    {
        private readonly DiscordSocketClient _client;

        public GraveyardService(DiscordSocketClient client) 
        {
            _client = client;
        }

        public async Task<string> AddGraveyardChannel(ulong channelId)
        {
            var newGraveyardChannel = new GraveyardChannel() { ChannelId = channelId };

            using var db = new IcarusContext();

            db.GraveyardChannels.Add(newGraveyardChannel);

            await db.SaveChangesAsync();

            return "Channel has been added.";
        }

        public async Task SendGraveyardMessage(PlayerCharacter character)
        {
            using var db = new IcarusContext();

            var channels = db.GraveyardChannels.ToList();

            foreach (var channel in channels)
            {
                try
                {
                    var discordChannel = (SocketTextChannel) _client.GetChannel(channel.ChannelId);

                    _ = discordChannel.SendMessageAsync($"{character.CharacterName} ({character.YearOfBirth}-{character.YearOfDeath})");
                }
                catch
                { 
                    // :)
                }
            }
        }
    }
}
