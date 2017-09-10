using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ELO_Bot.Commands.Admin
{
    [RequireTopic]
    [RequireContext(ContextType.Guild)]
    [CheckRegistered]
    public class AdminCommands : ModuleBase
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
        [CheckAdmin]
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
        [CheckAdmin]
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


        [Command("SetRegisterRole")]
        [Summary("SetRegisterRole <@role>")]
        [Remarks("Sets the role users will join when registering")]
        [CheckAdmin]
        public async Task SetReg(IRole role = null)
        {
            var embed = new EmbedBuilder();


            if (role == null)
            {
                embed.AddField("ERROR", "Please specify a role for users to be added to upon registering");
                embed.WithColor(Color.Red);
                await ReplyAsync("", false, embed.Build());
                return;
            }

            var server = ServerList.Load(Context.Guild);
            server.RegisterRole = role.Id;
            ServerList.Saveserver(server);
            embed.AddField("Complete!", $"Upon registering, users will now be added to the role: {role.Name}");
            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }

        [Command("ModifyLoss")]
        [Summary("ModifyLoss <points>")]
        [Remarks("Sets the servers Loss amount")]
        [CheckAdmin]
        public async Task Lose(int points)
        {
            var embed = new EmbedBuilder();
            var server = ServerList.Load(Context.Guild);
            if (points == 0)
            {
                embed.AddField("ERROR", "Please supply a number that isnt 0");
                embed.Color = Color.Red;
                await ReplyAsync("", false, embed.Build());
                return;
            }
            if (points <= 0)
                points = Math.Abs(points);
            server.Lossamount = points;
            ServerList.Saveserver(server);
            embed.AddField("Success", $"Upon losing, users will now lose {points} points");
            embed.Color = Color.Green;
            await ReplyAsync("", false, embed.Build());
        }

        [Command("ModifyWin")]
        [Summary("ModifyWin <points>")]
        [Remarks("Sets the servers Win amount")]
        [CheckAdmin]
        public async Task Win(int points)
        {
            var embed = new EmbedBuilder();
            var server = ServerList.Load(Context.Guild);
            if (points == 0)
            {
                embed.AddField("ERROR", "Please supply a number that isnt 0");
                embed.Color = Color.Red;
                await ReplyAsync("", false, embed.Build());
                return;
            }
            if (points <= 0)
                points = Math.Abs(points);
            server.Winamount = points;
            ServerList.Saveserver(server);
            embed.AddField("Success", $"Upon Winning, users will now gain {points} points");
            embed.Color = Color.Green;
            await ReplyAsync("", false, embed.Build());
        }
    }
}