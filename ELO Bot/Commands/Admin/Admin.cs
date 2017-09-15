using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ELO_Bot.Commands.Admin
{
    [RequireTopic]
    [RequireContext(ContextType.Guild)]
    [CheckRegistered]
    [CheckAdmin]
    public class Admin : ModuleBase
    {
        [Command("SetAnnouncements")]
        [Summary("SetAnnouncements")]
        [Remarks("Set the current channel for game announcements")]
        public async Task SetAnnounce()
        {
            var server = ServerList.Load(Context.Guild);
            server.AnnouncementsChannel = Context.Channel.Id;
            ServerList.Saveserver(server);
            await ReplyAsync("GameAnnouncements will now be posted in this channel");
        }

        [Command("SetAdmin")]
        [Summary("SetAdmin <@role>")]
        [Remarks("sets the configurable admin role")]
        public async Task SetAdmin(IRole adminrole)
        {
            var embed = new EmbedBuilder();

            var s1 = ServerList.Load(Context.Guild);

            s1.AdminRole = adminrole.Id;
            ServerList.Saveserver(s1);
            embed.AddField("Complete!", $"People with the role {adminrole.Id} can now use admin commands");
            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }

        [Command("SetWelcome")]
        [Summary("SetWelcome <message>")]
        [Remarks("sets the configurable welcome message")]
        public async Task SetWelcome([Remainder] string message = null)
        {
            var embed = new EmbedBuilder();

            var s1 = ServerList.Load(Context.Guild);

            if (message == null)
            {
                embed.AddField("ERROR", "Please specify a welcome message for users");
                embed.WithColor(Color.Red);
                await ReplyAsync("", false, embed.Build());
                return;
            }

            s1.Registermessage = message;
            ServerList.Saveserver(s1);
            embed.AddField("Complete!", $"Registration Message will now include the following:\n" +
                                        $"{message}");
            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }
    }
}