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

        [SlashCommand("create-character", "Creates a new character.")]
        public async Task CreateCharacter(string characterName, [Choice("20", 20), Choice("35", 35), Choice("50", 50)] int startingAge)
        {
            try
            {
                await _characterService.CreateNewCharacter(Context.User.Id.ToString(), characterName, startingAge);
                await ReplyAsync($"Character {characterName} has been created. Remember there are also commands for setting" +
                    $"your culture and career.");
            }
            catch(ExistingActiveCharacterException)
            {
                await ReplyAsync("Character could not be created as you still have an active character.");
            }
            catch(ArgumentException)
            {
                await ReplyAsync($"Character names may not be longer than 64 characters.");
            }
        }

        [SlashCommand("me", "Shows your active character or the active character of a person you ping.")]
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

        [SlashCommand("set-career", "Sets your own career.")]
        public async Task SetCareer(string career)
        {
            try
            {
                await _characterService.UpdateCharacterCareer(Context.User.Id.ToString(), career);
            }
            catch(ArgumentException)
            {
                await ReplyAsync("Career may not be longer than 64 characters.");
            }
            
            await ReplyAsync("Career set.");
        }

        [SlashCommand("set-assembly-rep", "Sets which group you represent in the assembly.")]
        public async Task SetCulture(string assembly)
        {
            try
            {
                await _characterService.UpdateCharacterAssembly(Context.User.Id.ToString(), assembly);
            }
            catch (ArgumentException)
            {
                await ReplyAsync("Assembly representation may not be longer than 64 characters.");
            }

            await ReplyAsync("Assembly representation set.");
        }
    }
}
