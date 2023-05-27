using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Exceptions;
using Icarus.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Icarus.Discord.Modules
{
    public class CharacterModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly CharacterService _characterService;

        public CharacterModule(CharacterService characterService) 
        {
            _characterService = characterService;
        }

        public async Task CreateCharacter(string characterName)
        {

        }

        [SlashCommand("Me", "Shows your active character or the active character of a person you ping.")]
        public async Task Me([Remainder]SocketGuildUser mention = null)
        {
            SocketGuildUser user = (SocketGuildUser)(mention ?? Context.User);
            try
            {
                var character = await _characterService.GetActiveCharacter(user.Id.ToString());

                var embedBuilder = new EmbedBuilder();

                embedBuilder.WithThumbnailUrl(user.GetDisplayAvatarUrl());
                embedBuilder.WithTitle(character.CharacterName);
                embedBuilder.AddField("Character Biography", character.CharacterDescription);

                await ReplyAsync(embed: embedBuilder.Build());
            }
            catch (NoActiveCharacterException)
            {
                await ReplyAsync($"Could not find an active character for {user.DisplayName}.");
            }
        }

        [SlashCommand("set-bio", "Sets your own bio.")]
        public async Task SetBio(string bio)
        {
            SocketGuildUser user = (SocketGuildUser) Context.User;

            try
            {
                await _characterService.UpdateCharacterBio(user.Id.ToString(), bio);
            }
            catch (NoActiveCharacterException)
            {
                await ReplyAsync($"Could not find an active character for {user.DisplayName}.");
            }

            await ReplyAsync($"Bio updated.");
        }
    }
}
