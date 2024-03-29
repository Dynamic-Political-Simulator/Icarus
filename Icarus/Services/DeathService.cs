﻿using Azure;
using Discord;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Services
{
    public class DeathService
    {
        private readonly CharacterService _characterService;
        private readonly GraveyardService _graveyardService;
        private readonly DebugService _debugService;
        private readonly TickService _tickService;
        private readonly MessagingService _messagingService;

        public DeathService(CharacterService characterService, 
            GraveyardService graveyardService, 
            DebugService debugService, 
            TickService tickService,
            MessagingService messagingService)
        {
            _characterService = characterService;
            _graveyardService = graveyardService;
            _debugService = debugService;
            _tickService = tickService;
            _messagingService = messagingService;

            _tickService.TickEvent += CheckDeathTimer;
        }

        public Task CheckDeathTimer()
        {
            using var db = new IcarusContext();

            var charactersOnTimer = db.DeathTimer.Where(c => c.TimeKilled < DateTime.UtcNow.AddMinutes(-1435)).ToList();

            foreach (var character in charactersOnTimer)
            {
                _ = KillCharacterById(character.CharacterId);
                db.DeathTimer.Remove(character);
            }

            db.SaveChangesAsync();

			return Task.CompletedTask;
        }

        public async Task OldAgeDeath(PlayerCharacter character)
        {
            using var db = new IcarusContext();

            var newDeathTimer = new DeathTimer()
            {
                CharacterId = character.CharacterId,
                TimeKilled = DateTime.UtcNow
            };

            _ = _messagingService.SendMessageToUser(ulong.Parse(character.DiscordUserId), "You feel your life force waning.You are certain you only have 24 hours left, it is best to deal with everything undone now before it's too late.");

            db.DeathTimer.Add(newDeathTimer);
            await db.SaveChangesAsync();

            _ = _debugService.PrintToChannels($"Character {character.CharacterName} has been added to the 24-hour death timer.");
        }

        public async Task<string> KillCharacterById(string characterId)
        {
            try
            {
                var activeCharacter = await _characterService.GetCharacter(characterId);

                using var db = new IcarusContext();

                var gameState = db.GameStates.First();

                activeCharacter.YearOfDeath = gameState.Year;

                db.Update(activeCharacter);
                await db.SaveChangesAsync();

                _ = _graveyardService.SendGraveyardMessage(activeCharacter);

                _ = _messagingService.SendMessageToUser(ulong.Parse(activeCharacter.DiscordUserId), $"{activeCharacter.CharacterName} has died.");

                return $"Press F for {activeCharacter.CharacterName}";
            }
            catch (NoActiveCharacterException)
            {
                return "Could not kill character. No active character found.";
            }
        }

        public async Task<string> KillCharacter(string discordId)
        {
            try
            {
                var activeCharacter = await _characterService.GetActiveCharacter(discordId);

                using var db = new IcarusContext();

                var gameState = db.GameStates.First();

                activeCharacter.YearOfDeath = gameState.Year;

                db.Update(activeCharacter);
                await db.SaveChangesAsync();

                _ = _graveyardService.SendGraveyardMessage(activeCharacter);

                _ = _messagingService.SendMessageToUser(ulong.Parse(activeCharacter.DiscordUserId), $"{activeCharacter.CharacterName} has died.");

                return $"Press F for {activeCharacter.CharacterName}";
            }
            catch (NoActiveCharacterException)
            {
                return "Could not kill character. No active character found.";
            }
        }
    }
}
