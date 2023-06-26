﻿using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Icarus.Discord.CustomPreconditions;
using Icarus.Exceptions;
using Icarus.Services;
using System;
using System.Threading.Tasks;

namespace Icarus.Discord.Modules
{
    public class CharacterModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly DiscordSocketClient _client;

        private readonly CharacterService _characterService;
        private readonly GoIService _goiService;
        private readonly RoleService _roleService;

        public CharacterModule(DiscordSocketClient client, CharacterService characterService, GoIService goIService, RoleService roleService) 
        {
            _client = client;
            _characterService = characterService;
            _goiService = goIService;
            _roleService = roleService;
        }

        [SlashCommand("create-character", "Creates a new character.")]
        [RequireProfile]
        public async Task CreateCharacter(string characterName, [Choice("20", 20), Choice("35", 35), Choice("50", 50)] int startingAge)
        {
            try
            {
                await _characterService.CreateNewCharacter(Context.User.Id.ToString(), characterName, startingAge);
                await RespondAsync(@$"Character {characterName} has been created. Remember there are also commands for setting
                    your character description (set-bio), culture (set-culture), and career (set-career).

                    You can also set your Group of Interest and your Privileged Group.

                    A Privileged Group (or PG for short) is the semi-organised entity which appointed you to the position of Notable. 
                    Their name, exact nature (a merchant house, a chamber of the Admiralty, an Olikost temple), and their ideological bend are entirely up to you, consider them part of your character's backstory.

                    That being said, all PG's also must fall within an empowered Group of Interest (GoI) which they reasonably correlate to. GoI's are overarching classifications of powerful interests such as Urban Guilds or Landed Estates. 
                    If a PG would not fit within any empowered GoI (for example pirates are a GoI but they represent various dregs and outlaws), then it simply means that you cannot make it. 
                    PG's must also not be overly powerful, you cannot create a PG which is canonicly the owner of half the land on an island - basically, keep it simple, keep it fair.
                    ");
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
                if (character.GoIid != null)
                {
                    embedBuilder.AddField("Assembly Representation", character.GroupOfInterest.Name);
                }
                if (character.PrivilegedGroup != null)
                {
                    embedBuilder.AddField("Privileged Group", character.PrivilegedGroup);
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
        [SlashCommand("set-culture", "Sets your culture.")]
        public async Task SetCulture(string assembly)
        {
            try
            {
                await _characterService.UpdateCharacterCulture(Context.User.Id.ToString(), assembly);
            }
            catch (ArgumentException)
            {
                await RespondAsync("Culture may not be longer than 64 characters.");
            }

            await RespondAsync("Culture set.");
        }

        [RequireProfile]
        [SlashCommand("set-goi", "Sets your Group of Interest.")]
        public async Task SetAssemblyRep()
        {
            var gois = await _goiService.GetAllGroups();

            var smb = new SelectMenuBuilder()
                .WithPlaceholder("Group of Interest")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithCustomId("char-goi-selection");
                
            foreach (var group in gois)
            {
                var smob = new SelectMenuOptionBuilder();

                smob.WithValue(group.Id.ToString());
                smob.WithLabel(group.Name);
            }

            var componentBuilder = new ComponentBuilder().WithSelectMenu(smb);

            await RespondAsync("Select your Group of Interest", components: componentBuilder.Build(), ephemeral: true);
        }
    }
}
