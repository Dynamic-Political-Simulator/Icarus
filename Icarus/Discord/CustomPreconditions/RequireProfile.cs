using Discord;
using Discord.Interactions;
using Icarus.Context;
using Icarus.Context.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Discord.CustomPreconditions
{
    public class RequireProfile : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            using var db = new IcarusContext();

            var hasProfile = await db.Users.SingleOrDefaultAsync(du => du.DiscordId == context.User.Id.ToString());

            if (hasProfile == null)
            {
                var newProfile = new DiscordUser()
                {
                    DiscordId = context.User.Id.ToString(),
                    CanUseAdminCommands = false
                };

                db.Users.Add(newProfile);
                await db.SaveChangesAsync();
            }

            return await Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
