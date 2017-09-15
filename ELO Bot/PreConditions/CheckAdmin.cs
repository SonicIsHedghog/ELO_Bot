using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ELO_Bot
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class CheckAdmin : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
            IServiceProvider prov)
        {
            var userisadmin = false;
            var s1 = ServerList.Load(context.Guild);
            if (s1.AdminRole != 0)
                if ((context.User as IGuildUser).RoleIds.Contains(s1.AdminRole))
                    userisadmin = true;
            if (!((context.User as IGuildUser).GuildPermissions.Administrator || userisadmin ||
                  context.User.Id == context.Guild.OwnerId))
                return await Task.FromResult(
                    PreconditionResult.FromError(
                        $"This Command requires admin permissions."));
            return await Task.FromResult(PreconditionResult.FromSuccess());
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class CheckRegistered : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
            IServiceProvider prov)
        {
            var s1 = ServerList.Load(context.Guild);

            try
            {
                if (s1.UserList.FirstOrDefault(x => x.UserId == context.User.Id) != null)
                    return await Task.FromResult(PreconditionResult.FromSuccess());

                return await Task.FromResult(
                    PreconditionResult.FromError(
                        $"You are not registered, type `=register <name>` to begin"));
            }
            catch
            {
                return await Task.FromResult(
                    PreconditionResult.FromError(
                        $"You are not registered, type `=register <name>` to begin"));
            }
        }
    }
}