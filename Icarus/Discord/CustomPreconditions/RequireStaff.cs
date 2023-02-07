using Discord.Commands;
using Icarus.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icarus.Discord.CustomPreconditions
{
    public class RequireStaff : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var db = services.GetService(typeof(IcarusContext)) as IcarusContext;

            var discordUser = db.Users.SingleOrDefault(du => du.DiscordId == context.User.Id.ToString());

            if (discordUser == null || !discordUser.CanUseAdminCommands)
            {
                return PreconditionResult.FromError("You don't have the required permissions to use this command.");
            }

            return PreconditionResult.FromSuccess();
        }
    }
}
