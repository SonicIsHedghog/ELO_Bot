using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace ELO_Bot.PreConditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireTopic : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command,
            IServiceProvider prov)
        {
            var istopisused = false;
            foreach (var channel in ((SocketGuild) context.Guild).Channels)
            {
                var t = channel as ITextChannel;
                var topic = "topic";
                try
                {
                    if (t != null && t.Topic != null)
                        topic = t.Topic;
                }
                catch
                {
                    //
                }
                if (topic.ToLower().Contains($"[{command.Name.ToLower()}]"))
                    istopisused = true;
            }

            if (!istopisused) return Task.FromResult(PreconditionResult.FromSuccess());

            if (context.Channel is ITextChannel textchannel && textchannel.Topic.ToLower().Contains($"[{command.Name.ToLower()}]"))
                return Task.FromResult(PreconditionResult.FromSuccess());
            return Task.FromResult(
                PreconditionResult.FromError(
                    $"Command is only available in channels containing `[{command.Name}]` in their topic"));
        }
    }
}