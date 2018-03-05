using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ELO_Bot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class CheckAdmin : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
            IServiceProvider prov)
        {
            try
            {
                var s1 = Servers.ServerList.First(x => x.ServerId == context.Guild.Id);

                var own = await context.Client.GetApplicationInfoAsync();
                if (own.Owner.Id == context.User.Id)
                    return await Task.FromResult(PreconditionResult.FromSuccess());

                if (true)
                    if (((IGuildUser) context.User).RoleIds.Contains(s1.AdminRole))
                        return await Task.FromResult(PreconditionResult.FromSuccess());

                if (!(((IGuildUser) context.User).GuildPermissions.Administrator ||
                      context.User.Id == context.Guild.OwnerId))
                    return await Task.FromResult(
                        PreconditionResult.FromError(
                            $"This Command requires admin permissions."));

                return await Task.FromResult(PreconditionResult.FromSuccess());
            }
            catch
            {
                var own = await context.Client.GetApplicationInfoAsync();
                if (own.Owner.Id == context.User.Id)
                    return await Task.FromResult(PreconditionResult.FromSuccess());

                if (((IGuildUser) context.User).GuildPermissions.Administrator)
                    return await Task.FromResult(PreconditionResult.FromSuccess());
                return await Task.FromResult(
                    PreconditionResult.FromError(
                        $"This Command requires admin permissions."));
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class CheckModerator : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
            IServiceProvider prov)
        {
            try
            {
                var s1 = Servers.ServerList.First(x => x.ServerId == context.Guild.Id);

                var own = await context.Client.GetApplicationInfoAsync();
                if (own.Owner.Id == context.User.Id)
                    return await Task.FromResult(PreconditionResult.FromSuccess());

                if (s1.ModRole != 0)
                    if (((IGuildUser) context.User).RoleIds.Contains(s1.ModRole))
                        return await Task.FromResult(PreconditionResult.FromSuccess());

                if (s1.AdminRole != 0)
                    if (((IGuildUser) context.User).RoleIds.Contains(s1.AdminRole))
                        return await Task.FromResult(PreconditionResult.FromSuccess());

                if (!(((IGuildUser) context.User).GuildPermissions.Administrator ||
                      context.User.Id == context.Guild.OwnerId))
                    return await Task.FromResult(
                        PreconditionResult.FromError(
                            "This Command requires Moderator OR Admin permissions."));

                return await Task.FromResult(PreconditionResult.FromSuccess());
            }
            catch
            {
                var own = await context.Client.GetApplicationInfoAsync();
                if (own.Owner.Id == context.User.Id)
                    return await Task.FromResult(PreconditionResult.FromSuccess());

                if (((IGuildUser) context.User).GuildPermissions.Administrator)
                    return await Task.FromResult(PreconditionResult.FromSuccess());
                return await Task.FromResult(
                    PreconditionResult.FromError(
                        $"This Command requires Moderator OR Admin permissions."));
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class ServerOwner : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
            IServiceProvider prov)
        {
            var own = await context.Client.GetApplicationInfoAsync();
            if (own.Owner.Id == context.User.Id)
                return await Task.FromResult(PreconditionResult.FromSuccess());

            if (context.Guild.OwnerId == context.User.Id)
                return await Task.FromResult(PreconditionResult.FromSuccess());

            return await Task.FromResult(
                PreconditionResult.FromError(
                    "This Command can only be performed by the server owner"));
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class CheckRegistered : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
            IServiceProvider prov)
        {
            var s1 = Servers.ServerList.First(x => x.ServerId == context.Guild.Id);

            try
            {
                if (s1.UserList.FirstOrDefault(x => x.UserId == context.User.Id) != null)
                    return await Task.FromResult(PreconditionResult.FromSuccess());

                return await Task.FromResult(
                    PreconditionResult.FromError(
                        "You are not registered, type `=register <name>` to begin"));
            }
            catch
            {
                return await Task.FromResult(
                    PreconditionResult.FromError(
                        "You are not registered, type `=register <name>` to begin"));
            }
        }
    }
}
