using Discord;
using Discord.Interactions;
using Icarus.Discord.CustomPreconditions;
using Icarus.Exceptions;
using Icarus.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Discord.Modules
{
    public class PurseModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly PurseService _purseService;

        public PurseModule(PurseService purseService)
        {
            _purseService = purseService;
        }

        [SlashCommand("give-money", "Gives money.")]
        [RequireAdmin]
        public async Task GiveMoney()
        {
            var currencies = await _purseService.GetCurrencies();

            var smb = new SelectMenuBuilder()
                .WithPlaceholder("Currency")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithCustomId("give-money");

            foreach (var currency in currencies)
            {
                smb.AddOption(currency.Id.ToString(), currency.Name);
            }

            var userSelect = new SelectMenuBuilder("user-id", minValues: 1, maxValues: 1, type: ComponentType.UserSelect);
            var amountSelect = new TextInputBuilder("Amount", "currency-give-amount");
            var submitButton = new ButtonBuilder("Submit", "submit-button");
        }

        [SlashCommand("my-purse", "Shows your purse.")]
        public async Task MyPurse()
        {
            try
            {
                var result = await _purseService.GetPurse(Context.User.Id);

                var eb = new EmbedBuilder();

                eb.WithTitle("Purse");

                var sb = new StringBuilder();

                foreach(var item in result)
                {
                    sb.Append($"{item.CurrencyName}: {item.CurrencyAmount}");
                }

                if (sb.Length == 0)
                {
                    eb.WithDescription("You're broke.");
                }
                else
                {
                    eb.WithDescription(sb.ToString());
                }

                await RespondAsync(embed: eb.Build(), ephemeral: true);
            }
            catch (NoActiveCharacterException)
            {
                await RespondAsync("You do not have an active character.", ephemeral: true);
            }
        }
    }
}
