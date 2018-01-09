using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using ELO_Bot.Preconditions;

namespace ELO_Bot.Commands.Admin
{
    /// <summary>
    ///     to ensure no blacklisted commands are used
    ///     make sure that only admins can use these commands
    /// </summary>
    [CheckBlacklist]
    [CheckAdmin]
    public class Management : InteractiveBase<SocketCommandContext>
    {
        /// <summary>
        ///     deletes the specified user's profile.
        /// </summary>
        /// <param name="user">@user</param>
        /// <returns></returns>
        [Command("Deluser")]
        [Summary("DelUser <@user>")]
        [Remarks("deletes a user from the server's registered list")]
        public async Task DelUser(IUser user)
        {
            var embed = new EmbedBuilder();
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);


            foreach (var subject in server.UserList)
                if (subject.UserId == user.Id)
                {
                    server.UserList.Remove(subject);
                    break;
                }


            try
            {
                await ((IGuildUser) user).ModifyAsync(x => { x.Nickname = $"{user.Username}"; });
            }
            catch
            {
                embed.AddField("Error Modifying User Nickname",
                    "User has higher rank in server than bot and nickname cannot be edited.");
            }

            embed.AddField("User Removed", "All data has been flushed and name restored.");
            await ReplyAsync("", false, embed.Build());
        }

        /// <summary>
        ///     registers a user with the given username
        /// </summary>
        /// <param name="user">user to register</param>
        /// <param name="username">name for the user</param>
        /// <returns></returns>
        [Command("Registeruser")]
        [Summary("Registeruser <@user> <username>")]
        [Remarks("registers the user in the server")]
        public async Task Register(IUser user, [Remainder] string username = null)
        {
            var embed = new EmbedBuilder();

            if (username == null)
            {
                embed.AddField("ERROR", "Please specify a name to be registered with");
                embed.WithColor(Color.Red);
                await ReplyAsync("", false, embed.Build());
            }
            else
            {
                var usr = new Servers.Server.User
                {
                    UserId = user.Id,
                    Username = username,
                    Points = 0
                };

                var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
                if (server.UserList.Count >= 20)
                    if (!server.IsPremium)
                    {
                        embed.AddField("ERROR",
                            "Free User limit has been hit. To upgrade the limit from 20 users to unlimited users, Purchase premium here: https://rocketr.net/buy/0e79a25902f5");
                        await ReplyAsync("", false, embed.Build());
                        return;
                    }

                if (server.UserList.Any(member => member.UserId == user.Id))
                {
                    embed.AddField("ERROR", "User is already registered");
                    embed.WithColor(Color.Red);
                    await ReplyAsync("", false, embed.Build());
                    return;
                }
                server.UserList.Add(usr);
                embed.AddField($"{user.Username} registered as {username}", $"{server.Registermessage}");
                embed.WithColor(Color.Blue);
                try
                {
                    await UserRename(server.UsernameSelection, user, username, 0);
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
                            await ((IGuildUser) user).AddRoleAsync(serverrole);
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
        }

        /// <summary>
        ///     rename the specified user
        /// </summary>
        /// <param name="user">user to rename</param>
        /// <param name="newname">new name</param>
        /// <returns></returns>
        [Command("Rename")]
        [Summary("Rename <@user> <newname>")]
        [Remarks("Change a user's nickname")]
        public async Task Rename(IUser user, [Remainder] string newname = null)
        {
            var embed = new EmbedBuilder();


            if (newname == null)
            {
                embed.AddField("ERROR", "Please specify a new name for the user");
                embed.WithColor(Color.Red);
                await ReplyAsync("", false, embed.Build());
                return;
            }

            if (newname.Length > 20)
            {
                embed.AddField("ERROR", "Username length must be less than 20 characters");
                embed.WithColor(Color.Red);
                await ReplyAsync("", false, embed.Build());
                return;
            }

            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            var success = false;
            foreach (var member in server.UserList)
                if (member.UserId == user.Id)
                {
                    embed.AddField("User Renamed", $"{member.Username} => {newname}");
                    member.Username = newname;
                    success = true;
                    try
                    {
                        await UserRename(server.UsernameSelection, user, member.Username, member.Points);
                    }
                    catch
                    {
                        embed.AddField("ERROR",
                            "Server Nickname unable to be modified (bot has insufficient permissions)");
                    }
                    break;
                }

            if (success)
            {
                embed.WithColor(Color.Blue);
                await ReplyAsync("", false, embed.Build());
            }
            else
            {
                embed.AddField("ERROR", "User Not Registered");
                embed.WithColor(Color.Red);
                await ReplyAsync("", false, embed.Build());
            }
        }

        /// <summary>
        ///     bans the specified user from using the queue
        /// </summary>
        /// <param name="user">user to ban</param>
        /// <param name="i">hours to ban the user for.</param>
        /// <param name="reason"></param>
        /// <returns></returns>
        [Command("ban")]
        [Summary("ban <@user> <hours> <reason>")]
        [Remarks("ban user from using Lobbies for specified anount of hours")]
        public async Task Ban(SocketGuildUser user, int i, [Remainder] string reason = null)
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);

            var b = new Servers.Server.Ban
            {
                UserId = user.Id,
                Time = DateTime.UtcNow.AddHours(i),
                Reason = reason
            };

            if (server.Bans.Select(x => x.UserId == user.Id).Any())
                server.Bans.Remove(server.Bans.FirstOrDefault(x => x.UserId == user.Id));

            server.Bans.Add(b);

            foreach (var queue in server.Queue)
                if (queue.Users.Contains(user.Id) && !queue.IsPickingTeams)
                    queue.Users.Remove(user.Id);

            if (reason == null)
                await ReplyAsync($"**{user.Mention} has been banned for {i} hours from matchmaking**");
            else
                await ReplyAsync($"**{user.Mention} has been banned for {i} hours from matchmaking for:** {reason}");
        }

        /// <summary>
        ///     unbans the specified user from matchmaking
        /// </summary>
        /// <param name="user">user to unban</param>
        /// <returns></returns>
        [Command("unban")]
        [Summary("unban <@user>")]
        [Remarks("Unban the specified user")]
        public async Task Unban(SocketGuildUser user)
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);

            if (server.Bans.Select(x => x.UserId == user.Id).Any())
                server.Bans.Remove(server.Bans.FirstOrDefault(x => x.UserId == user.Id));

            await ReplyAsync($"{user.Mention} has been unbanned");
        }

        /// <summary>
        ///     lists all bans in the server
        ///     Also runs a check for users and removes expired bans
        /// </summary>
        /// <returns></returns>
        [Command("bans")]
        [Summary("bans")]
        [Remarks("List all bans in the server.")]
        public async Task Bans()
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            var embed = new EmbedBuilder();
            if (server.Bans.Count == 0 || server.Bans == null)
                embed.AddField("Bans", "There are no bans in the current server.");

            var toremove = new List<Servers.Server.Ban>();

            var desc = "";

            foreach (var user in server.Bans)
            {
                string u;
                try
                {
                    u = Context.Guild.GetUser(user.UserId).Mention;
                }
                catch
                {
                    u = $"[{user.UserId}]";
                }


                if (Math.Round((user.Time - DateTime.UtcNow).TotalMinutes, 0) < 0)
                {
                    toremove.Add(user);
                }
                else
                {
                    if (user.Reason != null)
                        desc +=
                            $"{u} `{Math.Round((user.Time - DateTime.UtcNow).TotalMinutes, 0)} Minutes Left` for: {user.Reason}\n";
                    else
                        desc +=
                            $"{u} `{Math.Round((user.Time - DateTime.UtcNow).TotalMinutes, 0)} Minutes Left`\n";
                }

                if (desc.Length <= 900) continue;
                embed.AddField("Bans", $"{desc}");
                desc = "";
            }

            if (desc != "")
                embed.AddField("Bans", desc);


            var removeddesc = "";
            if (toremove.Count > 0)
                foreach (var user in toremove)
                {
                    string u;
                    try
                    {
                        u = Context.Guild.GetUser(user.UserId).Mention;
                    }
                    catch
                    {
                        u = $"[Unavailable User]:{user.UserId}";
                    }

                    server.Bans.Remove(user);

                    removeddesc += $"{u}\n";

                    if (removeddesc.Length <= 900) continue;
                    embed.AddField("Unbanned Users", $"{removeddesc}");
                    removeddesc = "";
                }

            if (removeddesc != "")
                embed.AddField("Unbanned Users", removeddesc);


            embed.AddField("NOTE", "1. User Bans will automatically be removed once their time expires.\n" +
                                   "2. Names displayed as [123456789012345] are users who are no longer in the server");


            await ReplyAsync("", false, embed.Build());
        }

        /// <summary>
        ///     adds a new rank to the server
        /// </summary>
        /// <param name="points">points required to be given this rank</param>
        /// <param name="role">role name ie. @role</param>
        /// <returns></returns>
        [Command("addrank")]
        [Summary("addrank <points> <@role>")]
        [Remarks("set a role which people are given once reaching the specified points")]
        public async Task SetReg(int points, IRole role)
        {
            var embed = new EmbedBuilder();


            if (points == 0)
            {
                embed.AddField("ERROR", "Please specify a value greater than zero");
                await ReplyAsync("", false, embed.Build());
                return;
            }


            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            var add = new Servers.Server.Ranking
            {
                Points = points,
                RoleId = role.Id
            };

            var containedlevel = server.Ranks.SingleOrDefault(x => x.Points == points);
            if (containedlevel != null)
            {
                embed.AddField("ERROR", "The Ranks list already contains a role with this number of points");
                await ReplyAsync("", false, embed.Build());
                return;
            }

            var containedrole = server.Ranks.SingleOrDefault(x => x.RoleId == role.Id);
            if (containedrole != null)
            {
                var oldpoints = containedrole.Points;
                server.Ranks.Remove(containedrole);
                server.Ranks.Add(add);
                embed.AddField("Rank Updated", $"Points: {oldpoints} => {points}\n" +
                                               $"{role.Name}");
            }
            else
            {
                server.Ranks.Add(add);
                embed.AddField("Added", $"Rank: {role.Name}\n" +
                                        $"Points: {points}");
            }

            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }

        /// <summary>
        ///     remove the specified rank
        /// </summary>
        /// <param name="role">@role</param>
        /// <returns></returns>
        [Command("removerank")]
        [Summary("removerank <@role>")]
        [Remarks("remove a role from the Ranks list")]
        public async Task Remove(IRole role = null)
        {
            var embed = new EmbedBuilder();


            if (role == null)
            {
                embed.AddField("ERROR", "Please specify a role");
                await ReplyAsync("", false, embed.Build());
                return;
            }


            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);

            var containedrole = server.Ranks.SingleOrDefault(x => x.RoleId == role.Id);
            if (containedrole != null)
            {
                server.Ranks.Remove(containedrole);
                embed.AddField("SUCCESS", $"{role.Name} is no longer ranked");
            }
            else
            {
                embed.AddField("ERROR", "This role is not Ranked");
            }

            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }

        /// <summary>
        ///     remove the specified rank by the role ID, this prevents issues with deleted roles being ranked still.
        /// </summary>
        /// <param name="role">role ID</param>
        /// <returns></returns>
        [Command("removerank")]
        [Summary("removerank <RoleID>")]
        [Remarks("remove a role from the Ranks list by ID")]
        public async Task Remove(ulong role)
        {
            var embed = new EmbedBuilder();


            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);

            var containedrole = server.Ranks.SingleOrDefault(x => x.RoleId == role);
            if (containedrole != null)
            {
                server.Ranks.Remove(containedrole);
                embed.AddField("SUCCESS", $"{role} is no longer ranked");
            }
            else
            {
                embed.AddField("ERROR", "This role is not Ranked");
            }

            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }

        /// <summary>
        ///     specify a rankwinmodifier for the given rank
        ///     if a user has this rank and wins, their points will be modified according to this value rather than the server
        ///     default
        ///     set this value to 0 for the default server modifier to be used
        /// </summary>
        /// <param name="role">ranked role</param>
        /// <param name="points">points per win</param>
        /// <returns></returns>
        [Command("RankWinModifier")]
        [Summary("RankWinModifier <@role> <points>")]
        [Remarks("allow a specific ranks points to be modified differently to server default")]
        public async Task Rwm(IRole role, int points)
        {
            var embed = new EmbedBuilder();


            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);

            var containedrole = server.Ranks.SingleOrDefault(x => x.RoleId == role.Id);
            if (containedrole != null)
            {
                containedrole.WinModifier = Math.Abs(points);
                embed.AddField("SUCCESS", $"User points for the rank {role.Mention} will now be modified accordingly");
            }
            else
            {
                embed.AddField("ERROR", "This role is not Ranked");
            }

            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }

        /// <summary>
        ///     specify a ranklossmodifier for the given rank
        ///     if a user has this rank and loses, their points will be modified according to this value rather than the server
        ///     default
        ///     set this value to 0 to use the server default again
        /// </summary>
        /// <param name="role">ranked role</param>
        /// <param name="points">points per loss</param>
        /// <returns></returns>
        [Command("RankLossModifier")]
        [Summary("RankLossModifier <@role> <points>")]
        [Remarks("allow a specific ranks points to be modified differently to server default")]
        public async Task Rlm(IRole role, int points)
        {
            var embed = new EmbedBuilder();


            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);

            var containedrole = server.Ranks.SingleOrDefault(x => x.RoleId == role.Id);
            if (containedrole != null)
            {
                containedrole.LossModifier = Math.Abs(points);
                embed.AddField("SUCCESS", $"User points for the rank {role.Mention} will now be modified accordingly");
            }
            else
            {
                embed.AddField("ERROR", "This role is not Ranked");
            }

            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }

        /*
        /// <summary>
        ///     replaces a user in the current queue (for use if they are afk etc.)
        /// </summary>
        /// <param name="oldUser"></param>
        /// <param name="newUser"></param>
        /// <returns></returns>
        [Command("lobbyswitch")]
        [Summary("lobbyswitch <@olduser> <@newuser>")]
        [Remarks("replace the given user in the queue")]
        public async Task Sub(IUser oldUser, IUser newUser)
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            var embed = new EmbedBuilder();
            var queue = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            //get the current lobbies queue.
            if (queue != null)
            {
                if (queue.Users.Contains(newUser.Id))
                {
                    embed.AddField("ERROR", "User2 is already queued already queued");
                    await ReplyAsync("", false, embed.Build());
                    return;
                }

                if (queue.Users.Contains(oldUser.Id))
                {
                    queue.Users.Remove(oldUser.Id);
                    queue.Users.Add(newUser.Id);
                    embed.AddField("Success", $"{oldUser.Mention} has been replaced by {newUser.Mention}\n" +
                                              $"**[{queue.Users.Count}/{queue.UserLimit}]**");
                }
                else
                {
                    embed.AddField("ERROR", $"{oldUser.Mention} is not queued\n" +
                                            $"**[{queue.Users.Count}/{queue.UserLimit}]**");
                }


                await ReplyAsync("", false, embed.Build());
            }
            else
            {
                await ReplyAsync("Error: current channel is not a lobby.");
            }
        }

        [Command("gameswitch")]
        [Summary("gameswitch <match no.> <@OldUser> <@NewUser>")]
        [Remarks("replace the specified user in the given game.")]
        public async Task Replace(int gamenumber, IUser oldPlayer, IUser newPlayer)
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            var queue = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);

            if (queue == null)
            {
                await ReplyAsync("ERROR: Current Channel is not a lobby!");
                return;
            }

            var oldgame =
                server.Gamelist.FirstOrDefault(x => x.LobbyId == Context.Channel.Id && x.GameNumber == gamenumber);
            if (oldgame != null)
            {
                //check thay you are not already in the old game.
                if (oldgame.Team1.Contains(newPlayer.Id) || oldgame.Team2.Contains(newPlayer.Id))
                {
                    await ReplyAsync(
                        "new player is already in a team for the provided game. Only users that aren't in this game can replace others.");
                    return;
                }

                var oldprofile = server.UserList.FirstOrDefault(x => x.UserId == oldPlayer.Id);
                var newprofile = server.UserList.FirstOrDefault(x => x.UserId == newPlayer.Id);

                if (oldprofile == null || newprofile == null)
                {
                    await ReplyAsync(
                        "ERROR: One of these users is not registered.");
                    return;
                }

                if (oldgame.Team1.Contains(oldPlayer.Id))
                {
                    //remove specified user and replace with new user.
                    oldgame.Team1.Remove(oldPlayer.Id);
                    oldgame.Team1.Add(newPlayer.Id);

                    await ReplyAsync(
                        $"Game #{oldgame.GameNumber} Team 1: {oldPlayer.Mention} has been replaced by {newPlayer.Mention}");
                }
                if (oldgame.Team2.Contains(oldPlayer.Id))
                {
                    oldgame.Team2.Remove(oldPlayer.Id);
                    oldgame.Team2.Add(newPlayer.Id);

                    await ReplyAsync(
                        $"Game #{oldgame.GameNumber} Team 2: {oldPlayer.Mention} has been replaced by {newPlayer.Mention}");
                }



                var t1Mention = new List<string>();
                foreach (var u in oldgame.Team1)
                {
                    var use = server.UserList.FirstOrDefault(x => x.UserId == u)?.Username;
                    if (use != null && string.Equals(use, newprofile.Username, StringComparison.CurrentCultureIgnoreCase))
                    {
                        use = $"{oldprofile.Username} => {newprofile.Username}";
                    }
                    t1Mention.Add(use);
                }
                    

                var t2Mention = new List<string>();
                foreach (var u in oldgame.Team2)
                {
                    var use = server.UserList.FirstOrDefault(x => x.UserId == u)?.Username;
                    if (use != null && string.Equals(use, newprofile.Username, StringComparison.CurrentCultureIgnoreCase))
                    {
                        use = $"{oldprofile.Username} => {newprofile.Username}";
                    }
                    t2Mention.Add(use);
                }
                    

                var announcement = "**__Game Has Been Updated__**\n" +
                                   "**Lobby:** \n" +
                                   $"{Context.Channel.Name} - Match #{oldgame.GameNumber}\n" +
                                   $"**Team 1:** [{string.Join("\n", t1Mention)}]\n" +
                                   $"**Team 2**: [{string.Join("\n", t2Mention)}]\n" +
                                   $"When the game finishes, type `=game {Context.Channel.Name} {oldgame.GameNumber} <team1 or team2>`\n" +
                                   "This will modify each team's points respectively.";

                try
                {
                    var channel = await ((IGuild)Context.Guild).GetChannelAsync(server.AnnouncementsChannel);
                    await ((IMessageChannel)channel).SendMessageAsync(announcement);
                }
                catch
                {
                    await ReplyAsync(announcement);
                }
            }
            else
            {
                await ReplyAsync("Error: No game in this lobby with the specified match no.");
            }
        }*/

        /// <summary>
        ///     server owner only command, removes server users
        ///     removes server lobbies. bans and previous games as well.
        /// </summary>
        /// <returns></returns>
        [Command("ClearUsers", RunMode = RunMode.Async)]
        [Summary("ClearUsers")]
        [Remarks("Deletes ALL user profiles in the server (SERVER OWNER ONLY)")]
        [ServerOwner]
        public async Task ClearUsers()
        {
            var rnd = new Random().Next(0, 100);
            await ReplyAsync($"Reply with `{rnd}` to delete all user profiles for this server.\n" +
                             "NOTE: Profiles can NOT be recovered, this also removes bans, lobbies and previous games.");
            var nextmessage = await NextMessageAsync();
            if (!nextmessage.Content.Contains(rnd.ToString()))
            {
                await ReplyAsync("Clear has been aborted.");
                return;
            }
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            var modified = 0;
            var unmodified = 0;
            await ReplyAsync($"Users Being Pruned. Estimated time: {server.UserList.Count * 5} seconds");
            var iiterations = 0;
            foreach (var user in server.UserList)
            {
                iiterations++;
                if (iiterations % 20 == 0)
                    await ReplyAsync($"{Math.Ceiling((double) (iiterations * 100) / server.UserList.Count)}% complete");
                try
                {
                    var u = Context.Guild.GetUser(user.UserId);
                    await u.ModifyAsync(x => { x.Nickname = null; });
                    modified++;
                    await Task.Delay(5000);
                }
                catch
                {
                    //User unavailable
                    unmodified++;
                }
            }

            server.UserList = new List<Servers.Server.User>();
            server.Queue = new List<Servers.Server.Q>();
            server.Gamelist = new List<Servers.Server.PreviouMatches>();
            server.Bans = new List<Servers.Server.Ban>();

            await ReplyAsync("User prune completed.\n" +
                             $"Users Reset: {modified}\n" +
                             $"Users Unavailable: {unmodified}\n" +
                             "[NOTE]\n" +
                             "Server queues, game logs and lobbies have also been cleared.");
        }

        public async Task UserRename(int usernameSelection, IUser user, string username, int userpoints)
        {
            //await UserRename(server.UsernameSelection, u, user.Username, user.Points);
            if (usernameSelection == 1)
            {
                await ((IGuildUser) user).ModifyAsync(x => { x.Nickname = $"{userpoints} ~ {username}"; });

                if (CommandHandler.VerifiedUsers != null)
                    if (CommandHandler.VerifiedUsers.Contains(Context.User.Id))
                        await ((IGuildUser) user).ModifyAsync(x => { x.Nickname = $"👑{userpoints} ~ {username}"; });
            }
            else if (usernameSelection == 2)
            {
                await ((IGuildUser) user).ModifyAsync(x => { x.Nickname = $"[{userpoints}] {username}"; });

                if (CommandHandler.VerifiedUsers != null)
                    if (CommandHandler.VerifiedUsers.Contains(Context.User.Id))
                        await ((IGuildUser) user).ModifyAsync(x => { x.Nickname = $"👑[{userpoints}] {username}"; });
            }
            else if (usernameSelection == 3)
            {
                await ((IGuildUser) user).ModifyAsync(x => { x.Nickname = $"{username}"; });

                if (CommandHandler.VerifiedUsers != null)
                    if (CommandHandler.VerifiedUsers.Contains(Context.User.Id))
                        await ((IGuildUser) user).ModifyAsync(x => { x.Nickname = $"👑{username}"; });
            }
        }
    }
}