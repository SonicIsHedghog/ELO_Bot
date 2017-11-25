using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using ELO_Bot.PreConditions;
using Newtonsoft.Json;

namespace ELO_Bot.Commands
{
    public class User : InteractiveBase
    {
        /// <summary>
        ///     sign up for ELO Bot
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [Command("Register")]
        [Ratelimit(3, 30d, Measure.Seconds)]
        [Summary("Register <username>")]
        [Remarks("registers the user in the server")]
        public async Task Register([Remainder] string username = null)
        {
            var embed = new EmbedBuilder();

            if (username == null)
            {
                embed.AddField("ERROR", "Please specify a name to be registered with");
                embed.WithColor(Color.Red);
                await ReplyAsync("", false, embed.Build());
                return;
            }

            if (username.Length > 20)
            {
                embed.AddField("ERROR", "Username Must be 20 characters or less");
                embed.WithColor(Color.Red);
                await ReplyAsync("", false, embed.Build());
                return;
            }

            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);

            if (server.UserList.Count >= 20 && !server.IsPremium)
            {
                embed.AddField("ERROR",
                    "Free User limit has been hit. To upgrade the limit from 20 users to unlimited users, Purchase premium here: https://rocketr.net/buy/0e79a25902f5");
                await ReplyAsync("", false, embed.Build());
                return;
            }

            try
            {
                if (server.UserList.Any(member => member.UserId == Context.User.Id))
                {
                    var userprofile = server.UserList.FirstOrDefault(x => x.UserId == Context.User.Id);

                    if (userprofile == null)
                    {
                        await ReplyAsync("ERROR: User not registered!");
                        return;
                    }

                    if (!((IGuildUser)Context.User).RoleIds.Contains(server.RegisterRole) && server.RegisterRole != 0)
                        try
                        {
                            var serverrole = Context.Guild.GetRole(server.RegisterRole);
                            try
                            {
                                await ((IGuildUser)Context.User).AddRoleAsync(serverrole);
                            }
                            catch
                            {
                                embed.AddField("ERROR", "User Role Unable to be modified");
                            }
                        }
                        catch
                        {
                            embed.AddField("ERROR", "Register Role is Unavailable");
                        }

                    try
                    {
                        await ((IGuildUser)Context.User).ModifyAsync(x =>
                       {
                           x.Nickname = $"{userprofile.Points} ~ {username}";
                       });

                        userprofile.Username = username;
                    }
                    catch
                    {
                        embed.AddField("ERROR", "Username Unable to be modified (Permissions are above the bot)");
                    }


                    embed.AddField("ERROR", "User is already registered, role and name have been updated accordingly");

                    embed.WithColor(Color.Red);

                    await ReplyAsync("", false, embed.Build());
                    return;
                }
            }
            catch
            {
                //
            }


            var user = new Servers.Server.User
            {
                UserId = Context.User.Id,
                Username = username,
                Points = 0
            };

            server.UserList.Add(user);
            embed.AddField($"{Context.User.Username} registered as {username}", $"{server.Registermessage}");
            embed.WithColor(Color.Blue);
            try
            {
                await ((IGuildUser)Context.User).ModifyAsync(x => { x.Nickname = $"0 ~ {username}"; });
            }
            catch
            {
                embed.AddField("ERROR", "Username Unable to be modified (Permissions are above the bot)");
            }
            if (server.RegisterRole != 0)
                try
                {
                    var serverrole = Context.Guild.GetRole(server.RegisterRole);
                    try
                    {
                        await ((IGuildUser)Context.User).AddRoleAsync(serverrole);
                    }
                    catch
                    {
                        embed.AddField("ERROR", "User Role Unable to be modified");
                    }
                }
                catch
                {
                    embed.AddField("ERROR", "Register Role is Unavailable");
                }
            await ReplyAsync("", false, embed.Build());
        }

        /// <summary>
        ///     get a user's profile and information
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [Command("GetUser")]
        [Summary("GetUser <@user>")]
        [Remarks("checks stats about a user")]
        public async Task GetUser(IUser user)
        {
            var embed = new EmbedBuilder();
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            var userlist = server.UserList;
            var orderlist = server.UserList.OrderBy(x => x.Points).Reverse().ToList();
            foreach (var usr in userlist)
                if (usr.UserId == user.Id)
                {
                    embed.AddField($"{usr.Username}", $"Points: {usr.Points}\n" +
                                                      $"Wins: {usr.Wins}\n" +
                                                      $"Losses: {usr.Losses}\n" +
                                                      $"Leaderboard Rank: {orderlist.FindIndex(x => x.UserId == user.Id) + 1}");
                    await ReplyAsync("", false, embed.Build());
                    return;
                }

            await ReplyAsync("User Unavailable");
        }

        /// <summary>
        ///     list users in order of hihest points
        ///     most wins
        ///     or most losses
        /// </summary>
        [Command("Leaderboard")]
        [Summary("Leaderboard <wins, losses, points>")]
        [Remarks("Displays Rank Leaderboard (Top 20 )")]
        public async Task LeaderBoard(string arg = null)
        {
            var embed = new EmbedBuilder();
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            var desc = "";
            try
            {
                if (arg != null && arg.Contains("win"))
                {
                    var orderlist = server.UserList.OrderBy(x => x.Wins).Reverse().ToList();

                    var i = 0;
                    foreach (var user in orderlist)
                    {
                        i++;
                        if (i <= 20)
                            desc += $"{i}. {user.Username} - {user.Wins}\n";
                    }
                    embed.WithFooter(x =>
                    {
                        x.Text = $"Usercount = {i}";
                        x.IconUrl = Context.Client.CurrentUser.GetAvatarUrl();
                    });
                    embed.AddField("LeaderBoard Wins", desc);
                    embed.Color = Color.Blue;
                    await ReplyAsync("", false, embed.Build());
                }
                else if (arg != null && arg.Contains("los"))
                {
                    var orderlist = server.UserList.OrderBy(x => x.Losses).Reverse().ToList();

                    var i = 0;
                    foreach (var user in orderlist)
                    {
                        i++;
                        if (i <= 20)
                            desc += $"{i}. {user.Username} - {user.Losses}\n";
                    }
                    embed.WithFooter(x =>
                    {
                        x.Text = $"Usercount = {i}";
                        x.IconUrl = Context.Client.CurrentUser.GetAvatarUrl();
                    });
                    embed.AddField("LeaderBoard Losses", desc);
                    embed.Color = Color.Blue;
                    await ReplyAsync("", false, embed.Build());
                }
                else
                {
                    var orderlist = server.UserList.OrderBy(x => x.Points).Reverse().ToList();

                    var i = 0;
                    foreach (var user in orderlist)
                    {
                        i++;
                        if (i <= 20)
                            desc += $"{i}. {user.Username} - {user.Points}\n";
                    }
                    embed.WithFooter(x =>
                    {
                        x.Text = $"Usercount = {i}";
                        x.IconUrl = Context.Client.CurrentUser.GetAvatarUrl();
                    });
                    embed.AddField("LeaderBoard Points", desc);
                    embed.Color = Color.Blue;
                    await ReplyAsync("", false, embed.Build());
                }
            }
            catch
            {
                var orderlist = server.UserList.OrderBy(x => x.Points).Reverse().ToList();

                var i = 0;
                foreach (var user in orderlist)
                {
                    i++;
                    if (i <= 20)
                        desc += $"{i}. {user.Username} - {user.Points}\n";
                }
                embed.WithFooter(x =>
                {
                    x.Text = $"Usercount = {i}";
                    x.IconUrl = Context.Client.CurrentUser.GetAvatarUrl();
                });
                embed.AddField("LeaderBoard Points", desc);
                embed.Color = Color.Blue;
                await ReplyAsync("", false, embed.Build());
            }
        }

        /// <summary>
        ///     display ranks for the current server
        /// </summary>
        /// <returns></returns>
        [Command("ranks")]
        [Summary("ranks")]
        [Remarks("display all Ranked Roles")]
        public async Task List()
        {
            var embed = new EmbedBuilder();
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            var orderedlist = server.Ranks.OrderBy(x => x.Points).Reverse();
            var desc = "Points - Role (PPW/PPL)\n";
            foreach (var lev in orderedlist)
            {
                string rolename;
                try
                {
                    rolename = Context.Guild.GetRole(lev.RoleId).Name;
                }
                catch
                {
                    rolename = $"ERR: {lev.RoleId}";
                }

                var pwin = lev.WinModifier == 0 ? server.Winamount : lev.WinModifier;
                var ploss = lev.LossModifier == 0 ? server.Lossamount : lev.LossModifier;

                desc += $"`{lev.Points}` - {rolename} (+{pwin}/-{ploss})\n";
            }

            if (server.RegisterRole != 0)
            {
                string rolename;
                try
                {
                    rolename = Context.Guild.GetRole(server.RegisterRole).Name;
                }
                catch
                {
                    rolename = $"ERR: {server.RegisterRole}";
                }

                var pwin = server.Winamount;
                var ploss = server.Lossamount;

                desc += $"`0` - {rolename} (+{pwin}/-{ploss})\n";
            }

            desc += "\n\nPPW = Points per win\n" +
                    "PPL = Points per loss";

            embed.AddField("Ranks", desc);
            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }

        /// <summary>
        ///     generate an invite for the bot to join your own server
        /// </summary>
        /// <returns></returns>
        [Command("Invite")]
        [Summary("Invite")]
        [Remarks("Invite the Bot")]
        public async Task Invite()
        {
            await ReplyAsync(
                $"Invite ELO Bot Here: <https://discordapp.com/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&scope=bot&permissions=2146958591>\n" +
                "Support Server: <https://discord.gg/ZKXqt2a>\n" +
                "Developed By: PassiveModding");
        }
    }
}