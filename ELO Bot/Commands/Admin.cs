using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ELO_Bot.Commands
{
    [RequireTopic]
    [RequireContext(ContextType.Guild)]
    [CheckRegistered]
    public class Admin : ModuleBase
    {
        [Command("AddPoints")]
        [Summary("AddPoints <points> <users>")]
        [Remarks("add points to the specified users")]
        [CheckAdmin]
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
        [Remarks("add points to the specified users")]
        [CheckAdmin]
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
        [Remarks("add points to the specified users")]
        [CheckAdmin]
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

        [Command("Deluser")]
        [Summary("DelUser <@user>")]
        [Remarks("deletes a user from the server's registered list")]
        [CheckAdmin]
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
                await (user as IGuildUser).ModifyAsync(x => { x.Nickname = $"{user.Username}"; });
            }
            catch
            {
                embed.AddField("Error Modifying User Nickname",
                    "User has higher rank in server than bot and nickname cannot be edited.");
            }

            embed.AddField("User Removed", "All data has been flushed and name restored.");
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

        [Command("Registeruser")]
        [Summary("Registeruser <@user> <username>")]
        [Remarks("registers the user in the server")]
        public async Task Register(IUser user, string username = null)
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
                    await (user as IGuildUser).ModifyAsync(x => { x.Nickname = $"0 ~ {username}"; });
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
                            await (user as IGuildUser).AddRoleAsync(serverrole);
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
        [CheckAdmin]
        public async Task Rename(IUser user, string newname = null)
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
                        await (user as IGuildUser).ModifyAsync(x =>
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
                embed.AddField("ERROR", $"User Not Registered");
                embed.WithColor(Color.Red);
                await ReplyAsync("", false, embed.Build());
            }


        }

        [Command("Clear")]
        [Summary("Clear")]
        [Remarks("Clear The Queue")]
        [CheckAdmin]
        public async Task ClearQueue()
        {
            var server = ServerList.Load(Context.Guild);
            var q = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            q.Users = new List<ulong>();
            ServerList.Saveserver(server);
            await ReplyAsync($"{Context.Channel.Name}'s queue has been cleared!");
        }

        [Command("CreateLobby")]
        [Summary("CreateLobby")]
        [Remarks("Turn The current channel into a lobby")]
        [CheckRegistered]
        public async Task LobbyCreate()
        {
            try
            {
                var server = ServerList.Load(Context.Guild);
                var embed = new EmbedBuilder();
                try
                {
                    var lobbyexists = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
                    if (lobbyexists == null)
                    {
                        server.Queue.Add(new ServerList.Server.Q
                        {
                            ChannelId = Context.Channel.Id,
                            Users = new List<ulong>()
                        });
                        embed.AddField("LOBBY CREATED", $"Lobby Name: {Context.Channel.Name}");
                    }
                    else
                    {
                        embed.AddField("ERROR", "This channel is already a lobby!");
                    }

                }
                catch
                {
                    var newlobby = new ServerList.Server.Q
                    {
                        ChannelId = Context.Channel.Id,
                        Users = new List<ulong>()
                    };
                    try
                    {
                        server.Queue.Add(newlobby);
                    }
                    catch
                    {
                        server.Queue = new List<ServerList.Server.Q> { newlobby };
                    }

                    embed.AddField("LOBBY CREATED", $"Lobby Name: {Context.Channel.Name}");
                }
                ServerList.Saveserver(server);
                await ReplyAsync("", false, embed.Build());
            }
            catch (Exception e)
            {
                await ReplyAsync(e.ToString());
            }
        }

        [Command("RemoveLobby")]
        [Summary("RemoveLobby")]
        [Remarks("Remove A Lobby")]
        [CheckAdmin]
        public async Task ClearLobby()
        {
            var server = ServerList.Load(Context.Guild);
            var q = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            server.Queue.Remove(q);
            ServerList.Saveserver(server);
            await ReplyAsync($"{Context.Channel.Name} is no longer a lobby!");
        }

        [Command("AddMap")]
        [Summary("AddMap <MapName>")]
        [Remarks("Add A Map")]
        [CheckAdmin]
        public async Task AddMap(string mapName)
        {
            var server = ServerList.Load(Context.Guild);
            if (!server.Maps.Contains(mapName))
            {
                server.Maps.Add(mapName);
                await ReplyAsync($"Map added {mapName}");

                ServerList.Saveserver(server);
            }
            else
            {
                await ReplyAsync($"Map Already Exists {mapName}");
            }
        }

        [Command("DelMap")]
        [Summary("DelMap <MapName>")]
        [Remarks("Delete A Map")]
        [CheckAdmin]
        public async Task DeleteMap(string mapName)
        {
            var server = ServerList.Load(Context.Guild);
            if (server.Maps.Contains(mapName))
            {
                server.Maps.Remove(mapName);
                await ReplyAsync($"Map Removed {mapName}");
                ServerList.Saveserver(server);
            }
            else
            {
                await ReplyAsync($"Map doesnt exist {mapName}");
            }
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
        [CheckRegistered]
        public class WinLoss : ModuleBase
        {
            [Command("win")]
            [Summary("Win <users>")]
            [Remarks("Add a win + win points for the specified users")]
            [CheckAdmin]
            public async Task Win(params IUser[] userlist)
            {
                var embed = new EmbedBuilder();
                var server = ServerList.Load(Context.Guild);
                var points = server.Winamount;
                if (!(server.Winamount > 0))
                {
                    embed.AddField("ERROR", "You have not setup the server's win modifier yet");
                    await ReplyAsync("", false, embed.Build());
                    return;
                }

                foreach (var user in userlist)
                    await WinLossPoints(server, user, true, points);
            }

            [Command("Lose")]
            [Summary("Lose <users>")]
            [Remarks("Add a loss to the specified users")]
            [CheckAdmin]
            public async Task Lose(params IUser[] userlist)
            {
                var embed = new EmbedBuilder();
                var server = ServerList.Load(Context.Guild);
                var points = server.Winamount;

                if (!(server.Lossamount > 0))
                {
                    embed.AddField("ERROR", "You have not setup the server's loss modifier yet");
                    await ReplyAsync("", false, embed.Build());
                    return;
                }

                foreach (var user in userlist)
                    await WinLossPoints(server, user, false, points);
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

            public async Task WinLossPoints(ServerList.Server server, IUser user, bool win, int points)
            {
                var embed = new EmbedBuilder();
                foreach (var usr in server.UserList)
                    if (user.Id == usr.UserId)
                    {
                        if (win)
                        {
                            usr.Points = usr.Points + points;
                            usr.Wins++;
                            embed.AddField($"{usr.Username} WON", $"User Won\n" +
                                                                  $"Points: {usr.Points}\n" +
                                                                  $"Wins: {usr.Wins}\n" +
                                                                  $"Losses: {usr.Losses}");
                            embed.Color = Color.Green;
                        }
                        else
                        {
                            points = Math.Abs(points);
                            usr.Points = usr.Points - points;
                            usr.Losses++;
                            if (usr.Points < 0)
                                usr.Points = 0;
                            embed.AddField($"{usr.Username} LOST", $"User Lost\n" +
                                                                   $"Points: {usr.Points}\n" +
                                                                   $"Wins: {usr.Wins}\n" +
                                                                   $"Losses: {usr.Losses}");
                            embed.Color = Color.Red;
                        }
                        try
                        {
                            await (user as IGuildUser).ModifyAsync(x => { x.Nickname = $"{usr.Points} ~ {usr.Username}"; });
                        }
                        catch
                        {
                            //
                        }
                        await CheckRank(server, user, usr);
                        break;
                    }
                ServerList.Saveserver(server);
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
        }

        public class Ranking : ModuleBase
        {
            [Command("addrank")]
            [Summary("addrank <points> <@role>")]
            [Remarks("set a Rank to join")]
            [CheckAdmin]
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
            [CheckAdmin]
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
        }
    }
}