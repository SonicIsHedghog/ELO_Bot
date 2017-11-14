using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ELO_Bot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class CheckBlacklist : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
            IServiceProvider prov)
        {
            var server = Servers.ServerList.First(x => x.ServerId == context.Guild.Id);
            var own = await context.Client.GetApplicationInfoAsync();

            if (command.Name.ToLower().Contains("blacklist"))
                return await Task.FromResult(PreconditionResult.FromSuccess());
            if (own.Owner.Id == context.User.Id)
                return await Task.FromResult(PreconditionResult.FromSuccess());

            if (server.ModRole != 0)
                if (((IGuildUser) context.User).RoleIds.Contains(server.ModRole))
                    return await Task.FromResult(PreconditionResult.FromSuccess());

            if (server.AdminRole != 0)
                if (((IGuildUser) context.User).RoleIds.Contains(server.AdminRole))
                    return await Task.FromResult(PreconditionResult.FromSuccess());

            if (server.CmdBlacklist.Contains(command.Name.ToLower()))
                return await Task.FromResult(
                    PreconditionResult.FromError(
                        $"This is a Blacklisted Command."));


            return await Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}