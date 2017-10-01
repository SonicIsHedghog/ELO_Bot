using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ELO_Bot.PreConditions;

namespace ELO_Bot.Commands.Admin
{
    [CheckAdmin]
    public class Management : ModuleBase<SocketCommandContext>
    {
        [Command("SetRegisterRole")]
        [Summary("SetRegisterRole <@role>")]
        [Remarks("Sets the role users will join when registering")]
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

        /*[Command("RegisterAll", RunMode = RunMode.Async)]
        [Summary("RegisterAll <role>")]
        [Remarks("Register All Users in the server or all users in the specified role")]
        public async Task RegisterAll(SocketRole specifiedRole = null)
        {
            var server = ServerList.Load(Context.Guild);
            var userlist = server.UserList;
            var newusers = new List<ServerList.Server.User>();
            IRole role = null;
            if (server.RegisterRole != 0)
            {
                role = Context.Guild.GetRole(server.RegisterRole);
            }
            var usergroup = Context.Guild.Users;
            if (specifiedRole != null)
            {
                var specifiedusers = Context.Guild.Users.Where(user => user.Roles.Contains(specifiedRole)).ToList();
                usergroup = specifiedusers;
            }
            

            foreach (var user in usergroup)
            {
                if (role != null)
                    {
                        try
                        {
                            await user.AddRoleAsync(role);
                        }
                        catch
                        {
                            //
                        }
                    }
                if (userlist.Any(x => x.UserId == user.Id)) continue;
                {
                    try
                    {
                        await user.ModifyAsync(x =>
                        {
                            x.Nickname = $"0 ~ {user.Username}";
                        });
                    }
                    catch
                    {
                        //
                    }
                    var newuser = new ServerList.Server.User
                    {
                        Username = user.Username,
                        Losses = 0,
                        UserId = user.Id,
                        Points = 0,
                        Wins = 0
                    };
                    newusers.Add(newuser);
                    await Task.Delay(1000);
                }
            }

            //These are split so that there is no issues between the (time consuming) user name and role modification
            //and actually modifying the database.
            var serv = ServerList.Load(Context.Guild);
            foreach (var user in newusers)
            {
                serv.UserList.Add(user);
            }
            await ReplyAsync($"All users have been registered.");
            ServerList.Saveserver(serv);
        }*/

        [Command("Deluser")]
        [Summary("DelUser <@user>")]
        [Remarks("deletes a user from the server's registered list")]
        public async Task DelUser(IUser user)
        {
            var embed = new EmbedBuilder();
            var server = ServerList.Load(Context.Guild);


            foreach (var subject in server.UserList)
                if (subject.UserId == user.Id)
                {
                    server.UserList.Remove(subject);
                    break;
                }

            ServerList.Saveserver(server);

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
                var usr = new ServerList.Server.User
                {
                    UserId = user.Id,
                    Username = username,
                    Points = 0
                };

                var server = ServerList.Load(Context.Guild);
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
                ServerList.Saveserver(server);
                embed.AddField($"{user.Username} registered as {username}", $"{server.Registermessage}");
                embed.WithColor(Color.Blue);
                try
                {
                    await ((IGuildUser) user).ModifyAsync(x => { x.Nickname = $"0 ~ {username}"; });
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

        [Command("Rename")]
        [Summary("Rename <@user>")]
        [Remarks("Sets the role users will join when registering")]
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

            var server = ServerList.Load(Context.Guild);
            var success = false;
            foreach (var member in server.UserList)
                if (member.UserId == user.Id)
                {
                    embed.AddField("User Renamed", $"{member.Username} => {newname}");
                    member.Username = newname;
                    success = true;
                    try
                    {
                        await ((IGuildUser) user).ModifyAsync(x =>
                        {
                            x.Nickname = $"{member.Points} ~ {member.Username}";
                        });
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
                ServerList.Saveserver(server);
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

        [Command("ban")]
        [Summary("ban <@user> <hours>")]
        [Remarks("stop a user from interacting with the queue for the specified amount of time")]
        public async Task Ban(SocketGuildUser user, int i)
        {
            var server = ServerList.Load(Context.Guild);
            var b = new ServerList.Server.Ban
            {
                UserId = user.Id,
                Time = DateTime.UtcNow.AddHours(i)
            };

            if (server.Bans.Select(x => x.UserId == user.Id).Any())
                server.Bans.Remove(server.Bans.FirstOrDefault(x => x.UserId == user.Id));

            server.Bans.Add(b);

            foreach (var queue in server.Queue)
                if (queue.Users.Contains(user.Id) && !queue.IsPickingTeams)
                    queue.Users.Remove(user.Id);

            await ReplyAsync($"{user.Mention} has been banned for {i} hours from matchmaking.");

            ServerList.Saveserver(server);
        }

        [Command("unban")]
        [Summary("unban <@user>")]
        [Remarks("Unban the specified user")]
        public async Task Unban(SocketGuildUser user)
        {
            var server = ServerList.Load(Context.Guild);

            if (server.Bans.Select(x => x.UserId == user.Id).Any())
                server.Bans.Remove(server.Bans.FirstOrDefault(x => x.UserId == user.Id));

            await ReplyAsync($"{user.Mention} has been unbanned");

            ServerList.Saveserver(server);
        }

        [Command("bans")]
        [Summary("bans")]
        [Remarks("List all bans in the server.")]
        public async Task Bans()
        {
            var server = ServerList.Load(Context.Guild);
            var embed = new EmbedBuilder();
            foreach (var user in server.Bans)
            {
                var u = Context.Guild.GetUser(user.UserId);
                embed.Description +=
                    $"{u.Mention} {Math.Round((user.Time - DateTime.UtcNow).TotalMinutes, 0)} Minutes Left\n";
            }
            embed.Description +=
                "\nNOTE: If a user's remaining minutes is negative, their ban will automatically be removed the next time they join the queue";
            await ReplyAsync("", false, embed.Build());
        }

        [Command("addrank")]
        [Summary("addrank <points> <@role>")]
        [Remarks("set a Rank to join")]
        public async Task SetReg(int points, IRole role)
        {
            var embed = new EmbedBuilder();


            if (points == 0)
            {
                embed.AddField("ERROR", "Please specify a value greater than zero");
                await ReplyAsync("", false, embed.Build());
                return;
            }


            var server = ServerList.Load(Context.Guild);
            var add = new ServerList.Server.Ranking
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

            ServerList.Saveserver(server);
            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }

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


            var server = ServerList.Load(Context.Guild);

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

            ServerList.Saveserver(server);
            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }

        [Command("removerank")]
        [Summary("removerank <RoleID>")]
        [Remarks("remove a role from the Ranks list by ID")]
        public async Task Remove(ulong role)
        {
            var embed = new EmbedBuilder();


            var server = ServerList.Load(Context.Guild);

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

            ServerList.Saveserver(server);
            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }


        [Command("ClearUsers", RunMode = RunMode.Async)]
        [Summary("ClearUsers")]
        [Remarks("Deletes ALL user profiles in the server.")]
        [ServerOwner]
        public async Task ClearUsers()
        {
            var server = ServerList.Load(Context.Guild);
            var modified = 0;
            var unmodified = 0;
            await ReplyAsync($"Users Being Pruned. Estimated time: {server.UserList.Count * 2} seconds");
            foreach (var user in server.UserList)
            {
                try
                {
                    var u = Context.Guild.GetUser(user.UserId);
                    await u.ModifyAsync(x =>
                    {
                        x.Nickname = null;
                    });
                    modified++;
                    await Task.Delay(2000);
                }
                catch
                {
                    //User unavailable
                    unmodified++;
                }
            }

            server.UserList = new List<ServerList.Server.User>();
            server.Queue = new List<ServerList.Server.Q>();
            server.Gamelist = new List<ServerList.Server.PreviouMatches>();
            server.Bans = new List<ServerList.Server.Ban>();
            ServerList.Saveserver(server);

            await ReplyAsync($"User prune completed.\n" +
                             $"Users Reset: {modified}\n" +
                             $"Users Unavailable: {unmodified}\n" +
                             $"[NOTE]\n" +
                             $"Server queues, game logs and lobbies have also been cleared.");
        }
    }
}