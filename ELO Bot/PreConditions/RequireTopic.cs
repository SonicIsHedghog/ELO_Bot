using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace ELO_Bot
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireTopic : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
            IServiceProvider prov)
        {
            var istopisused = false;
            foreach (var channel in (context.Guild as SocketGuild).Channels)
            {
                var t = channel as ITextChannel;
                var topic = "topic";
                try
                {
                    if (t.Topic != null)
                        topic = t.Topic;
                }
                catch
                {
                    //
                }
                if (topic.ToLower().Contains($"[{command.Name.ToLower()}]"))
                    istopisused = true;
            }

            if (istopisused)
            {
                var textchannel = context.Channel as ITextChannel;
                if (textchannel.Topic.ToLower().Contains($"[{command.Name.ToLower()}]"))
                    return Task.FromResult(PreconditionResult.FromSuccess());
                return Task.FromResult(
                    PreconditionResult.FromError(
                        $"Command is only available in channels containing `[{command.Name}]` in their topic"));
            }
            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}