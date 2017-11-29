using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using ELO_Bot.Preconditions;

namespace ELO_Bot.Commands.Admin
{
    /// <summary>
    ///     makes sure that only admins can run the following commands.
    /// </summary>
    [CheckBlacklist]
    [CheckAdmin]
    public class Point : ModuleBase
    {
        /// <summary>
        ///     adds the given points to the specified users.
        /// </summary>
        /// <param name="points">points to add</param>
        /// <param name="userlist">users ie. @user1 @user3 @user3....</param>
        /// <returns></returns>
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
                var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
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
                                await ((IGuildUser) user).ModifyAsync(x =>
                                {
                                    x.Nickname = $"{subject.Points} ~ {subject.Username}";
                                });

                                if (CommandHandler.VerifiedUsers != null)
                                {
                                    if (CommandHandler.VerifiedUsers.Contains(user.Id))
                                    {
                                        await ((IGuildUser) user).ModifyAsync(x =>
                                        {
                                            x.Nickname = $"👑{subject.Points} ~ {subject.Username}";
                                        });
                                    }
                                }
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
                embed.Color = Color.Green;
                await ReplyAsync("", false, embed.Build());
            }
        }

        /// <summary>
        ///     remove given points from the specified users.
        /// </summary>
        /// <param name="points">points to remove</param>
        /// <param name="userlist">users ie. @user1 @user3 @user3....</param>
        /// <returns></returns>
        [Command("DelPoints")]
        [Summary("DelPoints <points> <users>")]
        [Remarks("remove points from the specified users")]
        public async Task DelPoints(int points, params IUser[] userlist)
        {
            var embed = new EmbedBuilder();

            if (points <= 0)
                points = Math.Abs(points);
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
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
                            await ((IGuildUser) user).ModifyAsync(x =>
                            {
                                x.Nickname = $"{subject.Points} ~ {subject.Username}";
                            });
                            if (CommandHandler.VerifiedUsers != null)
                            {
                                if (CommandHandler.VerifiedUsers.Contains(user.Id))
                                {
                                    await ((IGuildUser) user).ModifyAsync(x => { x.Nickname = $"👑{subject.Points} ~ {subject.Username}"; });
                                }
                            }
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
            embed.Color = Color.Green;
            await ReplyAsync("", false, embed.Build());
        }

        /// <summary>
        ///     set the specific points of a user
        /// </summary>
        /// <param name="points">points value</param>
        /// <param name="users">list of users to modify the points for</param>
        /// <returns></returns>
        [Command("SetPoints")]
        [Summary("SetPoints <points> <user>")]
        [Remarks("set a user's exact points")]
        public async Task SetPoints(int points, params IUser[] users)
        {
            var embed = new EmbedBuilder();

            if (points <= 0)
                points = Math.Abs(points);
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);

            foreach (var u in users)
                try
                {
                    var user = server.UserList.First(x => x.UserId == u.Id);

                    user.Points = points;
                    if (user.Points < 0)
                        user.Points = 0;
                    try
                    {
                        await ((IGuildUser) u).ModifyAsync(x => { x.Nickname = $"{user.Points} ~ {user.Username}"; });
                        if (CommandHandler.VerifiedUsers != null)
                        {
                            if (CommandHandler.VerifiedUsers.Contains(u.Id))
                            {
                                await ((IGuildUser) u).ModifyAsync(x => { x.Nickname = $"👑{user.Points} ~ {user.Username}"; });
                            }
                        }

                        embed.AddField($"{user.Username} MODIFIED", $"Current Points: {user.Points}");
                    }
                    catch
                    {
                        embed.AddField($"{user.Username} MODIFIED",
                            $"Current Points: {user.Points}\n" +
                            $"{user.Username}'s username Unable to be modified (Permissions are above the bot)");
                    }
                }
                catch
                {
                    embed.AddField($"{u.Username} ERROR", "Not Registered");
                }


            embed.Color = Color.Green;
            await ReplyAsync("", false, embed.Build());
        }

        /// <summary>
        ///     check the rank of the specified user corresponds with their score.
        /// </summary>
        /// <param name="server">current server</param>
        /// <param name="user">user to modify</param>
        /// <param name="subject">user profile</param>
        /// <returns></returns>
        public async Task CheckRank(Servers.Server server, IUser user, Servers.Server.User subject)
        {
            foreach (var role in server.Ranks)
            {
                var u = user as IGuildUser;
                var r = Context.Guild.GetRole(role.RoleId);
                if (u != null && u.RoleIds.Contains(role.RoleId))
                    await u.RemoveRoleAsync(r);
            }
            try
            {
                var toprole = server.Ranks.Where(x => x.Points <= subject.Points).Max(x => x.Points);
                var top = server.Ranks.Where(x => x.Points == toprole);
                try
                {
                    var first = top.FirstOrDefault();
                    if (first != null)
                    {
                        var newrole = Context.Guild.GetRole(first.RoleId);
                        await ((IGuildUser) user).AddRoleAsync(newrole);
                    }
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

        /// <summary>
        ///     remove a single win from the provided users
        /// </summary>
        /// <param name="userlist"></param>
        /// <returns></returns>
        [Command("DelWin")]
        [Summary("DelWin <users>")]
        [Remarks("remove one win from the specified users")]
        public async Task DelWin(params IUser[] userlist)
        {
            var embed = new EmbedBuilder();

            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            foreach (var user in userlist)
            {
                var success = false;
                var userval = 0;
                foreach (var subject in server.UserList)
                    if (subject.UserId == user.Id)
                    {
                        subject.Wins = subject.Wins - 1;
                        if (subject.Wins < 0)
                            subject.Wins = 0;
                        success = true;
                        userval = subject.Wins;
                    }
                if (!success)
                    embed.AddField($"{user.Username} ERROR", "Not Registered");
                else
                    embed.AddField($"{user.Username} MODIFIED", "Removed: -1\n" +
                                                                $"Current wins: {userval}");
            }
            embed.Color = Color.Green;
            await ReplyAsync("", false, embed.Build());
        }

        /// <summary>
        ///     remove a single loss from the provided users
        /// </summary>
        /// <param name="userlist"></param>
        /// <returns></returns>
        [Command("DelLose")]
        [Summary("DelLose <users>")]
        [Remarks("remove a single loss from the specified users")]
        public async Task DelLose(params IUser[] userlist)
        {
            var embed = new EmbedBuilder();

            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            foreach (var user in userlist)
            {
                var success = false;
                var userval = 0;
                foreach (var subject in server.UserList)
                    if (subject.UserId == user.Id)
                    {
                        subject.Losses = subject.Losses - 1;
                        if (subject.Losses < 0)
                            subject.Losses = 0;
                        success = true;
                        userval = subject.Losses;
                    }
                if (!success)
                    embed.AddField($"{user.Username} ERROR", "Not Registered");
                else
                    embed.AddField($"{user.Username} MODIFIED", "Removed: -1\n" +
                                                                $"Current Losses: {userval}");
            }
            embed.Color = Color.Green;
            await ReplyAsync("", false, embed.Build());
        }
    }
}