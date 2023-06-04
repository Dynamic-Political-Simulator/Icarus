using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Discord.CustomPreconditions;
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
        [RequireProfile]
        public async Task CreateCharacter(string characterName, [Choice("20", 20), Choice("35", 35), Choice("50", 50)] int startingAge)
        {
            try
            {
                await _characterService.CreateNewCharacter(Context.User.Id.ToString(), characterName, startingAge);
                await RespondAsync($"Character {characterName} has been created. Remember there are also commands for setting" +
                    $" your culture and career.");
            }
            catch(ExistingActiveCharacterException)
            {
                await RespondAsync("Character could not be created as you still have an active character.");
            }
            catch(ArgumentException)
            {
                await RespondAsync($"Character names may not be longer than 64 characters.");
            }
        }

        [RequireProfile]
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

                if (character.CharacterDescription!= null)
                {
                    embedBuilder.AddField("Character Biography", character.CharacterDescription);
                }
                if (character.Culture != null)
                {
                    embedBuilder.AddField("Culture", character.Culture);
                }
                if (character.Career != null)
                {
                    embedBuilder.AddField("Career", character.Career);
                }
                if (character.AssemblyRepresentation != null)
                {
                    embedBuilder.AddField("Assembly Representation", character.AssemblyRepresentation);
                }


                await RespondAsync(embed: embedBuilder.Build());
            }
            catch (NoActiveCharacterException)
            {
                await RespondAsync($"Could not find an active character for {user.DisplayName}.");
            }
        }

        [RequireProfile]
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
                await RespondAsync($"Could not find an active character for {user.DisplayName}.");
            }

            await RespondAsync($"Bio updated.");
        }

        [RequireProfile]
        [SlashCommand("set-career", "Sets your own career.")]
        public async Task SetCareer(string career)
        {
            try
            {
                await _characterService.UpdateCharacterCareer(Context.User.Id.ToString(), career);
            }
            catch(ArgumentException)
            {
                await RespondAsync("Career may not be longer than 64 characters.");
            }
            
            await RespondAsync("Career set.");
        }

        [RequireProfile]
        [SlashCommand("set-assembly-rep", "Sets which group you represent in the assembly.")]
        public async Task SetCulture(string assembly)
        {
            try
            {
                await _characterService.UpdateCharacterAssembly(Context.User.Id.ToString(), assembly);
            }
            catch (ArgumentException)
            {
                await RespondAsync("Assembly representation may not be longer than 64 characters.");
            }

            await RespondAsync("Assembly representation set.");
        }
    }
}
