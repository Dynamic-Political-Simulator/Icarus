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
    public class RequireAdmin : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            using var db = new IcarusContext();

            var hasAdmin = await db.Users.SingleOrDefaultAsync(du => du.DiscordId == context.User.Id.ToString());

            if (!hasAdmin.CanUseAdminCommands)
            {
                return await Task.FromResult(PreconditionResult.FromError("Command requires command permissions."));
            }

            return await Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
