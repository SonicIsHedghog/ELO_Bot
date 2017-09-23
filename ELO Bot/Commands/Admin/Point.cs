using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ELO_Bot.Commands.Admin
{
    [CheckAdmin]
    public class Point : ModuleBase
    {
        [Command("AddPoints")]
        [Summary("AddPoints <points> <users>")]
        [Remarks("add points to the specified users")]
        public async Task AddPoints(int points, params IUser[] userlist)
        {
            var embed = new EmbedBuilder();
            if (points <= 0)
            {
                embed.AddField("ERROR", "This command is only for adding points");
                embed.Color = Color.Red;
                await ReplyAsync("", false, embed.Build());
            }
            else
            {
                var server = ServerList.Load(Context.Guild);
                foreach (var user in userlist)
                {
                    var success = false;
                    var userval = 0;
                    foreach (var subject in server.UserList)
                        if (subject.UserId == user.Id)
                        {
                            subject.Points = subject.Points + points;
                            success = true;
                            userval = subject.Points;
                            try
                            {
                                await (user as IGuildUser).ModifyAsync(x =>
                                {
                                    x.Nickname = $"{subject.Points} ~ {subject.Username}";
                                });
                            }
                            catch
                            {
                                embed.AddField("NAME ERROR",
                                    $"{user.Username}'s username Unable to be modified (Permissions are above the bot)");
                            }
                            await CheckRank(server, user, subject);
                        }

                    if (!success)
                        embed.AddField($"{user.Username} ERROR", "Not Registered");
                    else
                        embed.AddField($"{user.Username} MODIFIED", $"Added: +{points}\n" +
                                                                    $"Current Points: {userval}");
                }
                ServerList.Saveserver(server);
                embed.Color = Color.Green;
                await ReplyAsync("", false, embed.Build());
            }
        }

        [Command("DelPoints")]
        [Summary("DelPoints <points> <users>")]
        [Remarks("remove points from the specified users")]
        public async Task DelPoints(int points, params IUser[] userlist)
        {
            var embed = new EmbedBuilder();

            if (points <= 0)
                points = Math.Abs(points);
            var server = ServerList.Load(Context.Guild);
            foreach (var user in userlist)
            {
                var success = false;
                var userval = 0;
                foreach (var subject in server.UserList)
                    if (subject.UserId == user.Id)
                    {
                        subject.Points = subject.Points - points;
                        if (subject.Points < 0)
                            subject.Points = 0;
                        success = true;
                        userval = subject.Points;
                        try
                        {
                            await (user as IGuildUser).ModifyAsync(x =>
                            {
                                x.Nickname = $"{subject.Points} ~ {subject.Username}";
                            });
                        }
                        catch
                        {
                            embed.AddField("NAME ERROR",
                                $"{user.Username}'s username Unable to be modified (Permissions are above the bot)");
                        }
                        await CheckRank(server, user, subject);
                    }
                if (!success)
                    embed.AddField($"{user.Username} ERROR", "Not Registered");
                else
                    embed.AddField($"{user.Username} MODIFIED", $"Removed: -{points}\n" +
                                                                $"Current Points: {userval}");
            }
            ServerList.Saveserver(server);
            embed.Color = Color.Green;
            await ReplyAsync("", false, embed.Build());
        }

        [Command("SetPoints")]
        [Summary("SetPoints <points> <user>")]
        [Remarks("set a user's exact points")]
        public async Task DelPoints(int points, IUser user)
        {
            var embed = new EmbedBuilder();

            if (points <= 0)
                points = Math.Abs(points);
            var server = ServerList.Load(Context.Guild);
            var success = false;
            var userval = 0;

            foreach (var subject in server.UserList)
                if (subject.UserId == user.Id)
                {
                    subject.Points = points;
                    if (subject.Points < 0)
                        subject.Points = 0;
                    success = true;
                    userval = subject.Points;
                    try
                    {
                        await (user as IGuildUser).ModifyAsync(x =>
                        {
                            x.Nickname = $"{subject.Points} ~ {subject.Username}";
                        });
                    }
                    catch
                    {
                        embed.AddField("NAME ERROR",
                            $"{user.Username}'s username Unable to be modified (Permissions are above the bot)");
                    }
                    await CheckRank(server, user, subject);
                }
            if (!success)
                embed.AddField($"{user.Username} ERROR", "Not Registered");
            else
                embed.AddField($"{user.Username} MODIFIED", $"Current Points: {userval}");
            ServerList.Saveserver(server);
            embed.Color = Color.Green;
            await ReplyAsync("", false, embed.Build());
        }

        public async Task CheckRank(ServerList.Server server, IUser user, ServerList.Server.User subject)
        {
            foreach (var role in server.Ranks)
            {
                var u = user as IGuildUser;
                var r = Context.Guild.GetRole(role.RoleId);
                if (u.RoleIds.Contains(role.RoleId))
                    await u.RemoveRoleAsync(r);
            }
            try
            {
                var toprole = server.Ranks.Where(x => x.Points <= subject.Points).Max(x => x.Points);
                var top = server.Ranks.Where(x => x.Points == toprole);
                try
                {
                    var newrole = Context.Guild.GetRole(top.FirstOrDefault().RoleId);
                    await (user as IGuildUser).AddRoleAsync(newrole);
                }
                catch
                {
                    //role has been deleted
                }
            }
            catch
            {
                // No available roles
            }
        }

        [Command("ModifyLoss")]
        [Summary("ModifyLoss <points>")]
        [Remarks("Sets the servers Loss amount")]
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