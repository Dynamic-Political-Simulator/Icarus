using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Discord.CustomPreconditions;
using Icarus.Exceptions;
using Icarus.Services;
using System.Linq;
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

        [SlashCommand("link-pop-role", "Links a pop role to the bot.")]
        [RequireAdmin]
        public async Task LinkGoiRole(int popId, SocketRole role)
        {
            try
            {
                await _goiService.LinkGoiRole(popId, role.Id);
            }
            catch (GroupOfInterestNotFoundException )
            {
                await RespondAsync($"Could not find pop with ID {popId}.");
                return;
            }
            
            await RespondAsync($"Linked pop with ID {popId}, to role {role.Name}.");
        }

        [SlashCommand("create-pop", "Adds a new pop to the database.")]
        [RequireAdmin]
        public async Task AddGoI(string name)
        {
            await RespondAsync(await _goiService.AddGoI(name), ephemeral: true);
        }

        [SlashCommand("pops", "Lists all pops.")]
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

            if (gois.Count == 0) resultString = "No pops in database.";

            var eb = new EmbedBuilder();

            eb.WithTitle("Pops");
            eb.WithDescription(resultString);

            await RespondAsync(embed: eb.Build(), ephemeral: false);
        }

        [SlashCommand("character-pops", "Lists all characters grouped by assigned pops.")]
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

                var charactersWithGoi = result.Where(c => c.GoIid == goi.Id && c.YearOfDeath == -1).ToList().OrderBy(c => c.CharacterName);

                if (charactersWithGoi.Any())
                {
                    foreach (var line in charactersWithGoi)
                    {
                        sbLoop.AppendLine(line.CharacterName);
                    }
                }
                else
                {
                    sbLoop.AppendLine("No characters with this pop.");
                }
                

                newFieldLoop.WithValue(sbLoop.ToString());

                eb.AddField(newFieldLoop);
            }

            var newField = new EmbedFieldBuilder();
            newField.Name = "Without pop";

            var sb = new StringBuilder();

            var charactersWithNoGoi = result.Where(c => c.GoIid == null).ToList().OrderBy(c => c.CharacterName);

            if (charactersWithNoGoi.Any())
            {
                foreach (var line in charactersWithNoGoi)
                {
                    sb.AppendLine(line.CharacterName);
                }
            }
            else
            {
                sb.AppendLine("No characters without a pop.");
            }

            newField.WithValue(sb.ToString());

            eb.AddField(newField);

            await RespondAsync(embed: eb.Build(), ephemeral: false);
        }
    }
}
