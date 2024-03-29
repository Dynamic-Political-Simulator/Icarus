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
            await DeferAsync(ephemeral: true);

            var gois = await _goiService.GetAllGroups();

            var smb = new SelectMenuBuilder()
                .WithPlaceholder("Pop")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithCustomId("char-goi-selection");

            foreach (var group in gois)
            {
                var smob = new SelectMenuOptionBuilder();

                smob.WithValue(group.Id.ToString());
                smob.WithLabel(group.Name);

                smb.AddOption(smob);
            }

            var componentBuilder = new ComponentBuilder().WithSelectMenu(smb);

            try
            {
                await _characterService.CreateNewCharacter(Context.User.Id.ToString(), characterName, startingAge);
                _ = FollowupAsync(@$"Character {characterName} has been created. Remember there are also commands for setting your character description (set-bio), culture (set-culture), career (set-career), and PG (set-pg).

                    A Patronage Group (or PG for short) is the semi-organised entity which appointed you to the position of Notable. Their name, exact nature (a merchant house, a chamber of the Admiralty, an Olikost temple), and their ideological bend are entirely up to you, consider them part of your character's backstory. 
PG's must also not be overly powerful, you cannot create a PG which is canonically the owner of half the land on an island - basically, keep it simple, keep it fair. All available Pops are listed in the command.

                Set your Pop group:",
                ephemeral: true, components: componentBuilder.Build());
            }
            catch(ExistingActiveCharacterException)
            {
                await FollowupAsync("Character could not be created as you still have an active character.");
            }
            catch(ArgumentException)
            {
                await FollowupAsync($"Character names may not be longer than 64 characters.");
            }
        }

        [RequireProfile]
        [SlashCommand("me", "Shows your active character or the active character of a person you ping.")]
        public async Task Me([Remainder]SocketGuildUser mention = null)
        {
            await DeferAsync();

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
				embedBuilder.AddField("Age", await _characterService.GetCharacterAge(character.CharacterId));
                if (character.Culture != null)
                {
                    embedBuilder.AddField("Culture", character.Culture);
                }
                if (character.Career != null)
                {
                    embedBuilder.AddField("Career", character.Career);
                }
                if (character.PrivilegedGroup != null)
                {
                    embedBuilder.AddField("Patronage Group", character.PrivilegedGroup);
                }
                if (character.GoIid != null)
                {
                    embedBuilder.AddField("Pop", character.GroupOfInterest.Name);
				}

                await FollowupAsync(embed: embedBuilder.Build());
            }
            catch (NoActiveCharacterException)
            {
                await FollowupAsync($"Could not find an active character for {user.DisplayName}.");
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
                return;
            }
            catch (ArgumentException)
            {
                await RespondAsync("Cannot set a bio longer than 1024 characters.");
                return;
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
        [SlashCommand("set-pg", "Sets your Patronage Group.")]
        public async Task SetPG(string pg)
        {
            try
            {
                await _characterService.UpdateCharacterPG(Context.User.Id.ToString(), pg);
            }
            catch (ArgumentException)
            {
                await RespondAsync("PG may not be longer than 64 characters.");
            }

            await RespondAsync("PG set.");
        }

        [RequireProfile]
        [SlashCommand("set-pop", "Sets your Pop group.")]
        public async Task SetAssemblyRep()
        {
            var gois = await _goiService.GetAllGroups();

            var smb = new SelectMenuBuilder()
                .WithPlaceholder("Pop")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithCustomId("char-goi-selection");
                
            foreach (var group in gois)
            {
                var smob = new SelectMenuOptionBuilder();

                smob.WithValue(group.Id.ToString());
                smob.WithLabel(group.Name);

                smb.AddOption(smob);
            }

            var noneSmob = new SelectMenuOptionBuilder();
            noneSmob.WithValue("none");
            noneSmob.WithLabel("None");
            smb.AddOption(noneSmob);

            var componentBuilder = new ComponentBuilder().WithSelectMenu(smb);

            await RespondAsync("Select your Pop", components: componentBuilder.Build(), ephemeral: true);
        }
    }
}
