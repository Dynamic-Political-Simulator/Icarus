using Discord;
using Discord.Interactions;
using Icarus.Context;
using Icarus.Discord.CustomPreconditions;
using Icarus.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Discord.Modules
{
    public class GoIModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly CharacterService _characterService;
        private readonly GoIService _goiService;

        public GoIModule(CharacterService characterService, GoIService goiService)
        {
            _characterService = characterService;
            _goiService = goiService;
        }

        [SlashCommand("gois", "Lists all gois.")]
        [RequireAdmin]
        public async Task ListGroups()
        {
            var gois = await _goiService.GetAllGroups();

            var sb = new StringBuilder();

            foreach (var go in gois)
            {
                sb.AppendLine($"{go.Id}: {go.Name}");
            }

            var resultString = sb.ToString();

            if (resultString == "" || resultString == null) resultString = "No GoI's in database.";

            var eb = new EmbedBuilder();

            eb.WithTitle("Groups of Interest");
            eb.WithDescription(resultString);

            await ReplyAsync(embed: eb.Build());
        }

        [SlashCommand("character-gois", "Lists all characters grouped by assigned gois.")]
        [RequireAdmin]
        public async Task GetCharacterGoIList()
        {
            var result = await _characterService.GetAllCharactersIncludeGoI();

            var gois = await _goiService.GetAllGroups();

            var eb = new EmbedBuilder();

            foreach(var goi in gois)
            {
                var newFieldLoop = new EmbedFieldBuilder();
                newFieldLoop.Name = goi.Name;

                var sbLoop = new StringBuilder();

                var charactersWithGoi = result.Where(c => c.GoIid == goi.Id).ToList().OrderBy(c => c.CharacterName);

                foreach(var line in charactersWithGoi)
                {
                    sbLoop.AppendLine(line.CharacterName + ": " + line.GroupOfInterest.Name);
                }

                newFieldLoop.WithValue(sbLoop.ToString());

                eb.AddField(newFieldLoop);
            }

            var newField = new EmbedFieldBuilder();
            newField.Name = "Without GoI";

            var sb = new StringBuilder();

            var charactersWithNoGoi = result.Where(c => c.GoIid == null).ToList().OrderBy(c => c.CharacterName);

            foreach (var line in charactersWithNoGoi)
            {
                sb.AppendLine(line.CharacterName);
            }

            newField.WithValue(sb.ToString());

            eb.AddField(newField);

            await RespondAsync(embed: eb.Build());
        }
    }
}
