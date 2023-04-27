using Discord;
using Discord.Interactions;
using Icarus.Context;
using Icarus.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Icarus.Discord.CustomPreconditions
{
    public class RequireTokenAmount : PreconditionAttribute
    {
        private readonly ActionTokenType _tokenType;
        private readonly int _amount;
        private readonly bool _removeTokens;

        public RequireTokenAmount(ActionTokenType tokenType, int amount, bool removeTokens)
        {
            _tokenType = tokenType;
            _amount = amount;
            _removeTokens = removeTokens;
        }
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            var db = services.GetService(typeof(IcarusContext)) as IcarusContext;

            var character = db.Characters.FirstOrDefault(c => c.YearOfDeath == -1 && c.DiscordUserId == context.User.Id.ToString());

            if(character.Tokens.First(t => t.TokenType == _tokenType).Amount < _amount)
            {
                return await Task.FromResult(PreconditionResult.FromError($"You need {_amount} of {nameof(_tokenType)} to do this."));
            }

            if (_removeTokens)
            {
                var tokens = character.Tokens.First(t => t.TokenType == _tokenType);

                tokens.Amount -= _amount;

                db.Update(tokens);
                await db.SaveChangesAsync();
            }
            
            return await Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
