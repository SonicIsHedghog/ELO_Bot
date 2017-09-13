using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ELO_Bot.Commands
{
    [CheckRegistered]
    public class MatchMaking : ModuleBase
    {
        [Command("Maps")]
        [Summary("Maps")]
        [Remarks("List Maps")]
        public async Task Maps()
        {
            var embed = new EmbedBuilder();
            var server = ServerList.Load(Context.Guild);
            var lobby = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            foreach (var map in lobby.Maps)
                embed.Description += $"{map}\n";

            await ReplyAsync("", false, embed.Build());
        }

        [Command("Queue")]
        [Summary("Queue")]
        [Remarks("Display the current queue")]
        public async Task Queue()
        {
            var server = ServerList.Load(Context.Guild);
            var embed = new EmbedBuilder();
            try
            {
                var lobby = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
                if (lobby.IsPickingTeams)
                {
                    var t1List = "";
                    var t2List = "";
                    var users = "";

                    var cap1 = await Context.Guild.GetUserAsync(lobby.T2Captain);
                    var cap2 = await Context.Guild.GetUserAsync(lobby.T1Captain);

                    foreach (var us in lobby.Team1)
                    {
                        var u = await Context.Client.GetUserAsync(us);
                        t1List += $"{u.Mention} ";
                    }
                    foreach (var us in lobby.Team2)
                    {
                        var u = await Context.Client.GetUserAsync(us);
                        t2List += $"{u.Mention} ";
                    }
                    foreach (var us in lobby.Users)
                    {
                        var u = await Context.Client.GetUserAsync(us);
                        users += $"{u.Mention} ";
                    }
                    embed.AddField("Lobby", $"[{lobby.Team1.Count}/{lobby.UserLimit / 2}]\n" +
                                            $"Team1: {t1List}\n" +
                                            $"Team2: {t2List}\n" +
                                            "\nCaptains: \n" +
                                            $"1: {cap1.Mention}\n" +
                                            $"2: {cap2.Mention}\n" +
                                            $"Players Left: {users}");
                    await ReplyAsync("", false, embed.Build());
                    return;
                }
                if (lobby.Users.Count == 0)
                {
                    //empty
                    embed.AddField($"{Context.Channel.Name} Queue **[0/{lobby.UserLimit}]** #{lobby.Games + 1}",
                        $"Empty");
                }
                else
                {
                    //make list
                    var list = "";
                    foreach (var user in lobby.Users)
                    {
                        var subject = server.UserList.FirstOrDefault(x => x.UserId == user);
                        list += $"{subject.Username} - {subject.Points}\n";
                    }

                    embed.AddField(
                        $"{Context.Channel.Name} Queue **[{lobby.Users.Count}/{lobby.UserLimit}]** #{lobby.Games + 1}",
                        $"{list}");
                }
                embed.AddField("Join/Leave", "`=Join` To Join the queue\n" +
                                             "`=Leave` To Leave the queue");
            }
            catch
            {
                embed.AddField("Error", "The current channel is not a lobby, there is no queue here.");
            }


            await ReplyAsync("", false, embed.Build());
        }

        [Command("Lobby")]
        [Summary("Lobby")]
        [Remarks("Gamemode Info")]
        public async Task Lobby()
        {
            var server = ServerList.Load(Context.Guild);
            var embed = new EmbedBuilder();
            try
            {
                var lobbyexists = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);

                if (lobbyexists.ChannelGametype == null)
                    lobbyexists.ChannelGametype = "Unknown";

                embed.AddField("Lobby Info", $"**Player Limit:**\n" +
                                             $"{lobbyexists.UserLimit}\n" +
                                             $"**Game Number:**\n" +
                                             $"{lobbyexists.Games + 1}\n" +
                                             $"**Automatic Teams:**\n" +
                                             $"{!lobbyexists.Captains}\n" +
                                             $"**Gamemode Description:**\n" +
                                             $"{lobbyexists.ChannelGametype}");
            }
            catch
            {
                embed.AddField("Error", "The current channel is not a lobby, there is no queue here.");
            }

            await ReplyAsync("", false, embed.Build());
        }

        [Command("pick")]
        [Summary("pick <@user>")]
        [Remarks("Choose a player for your team")]
        public async Task Pick(IUser user)
        {
            var server = ServerList.Load(Context.Guild);
            var embed = new EmbedBuilder();

            ServerList.Server.Q lobby;
            try
            {
                lobby = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            }
            catch
            {
                embed.AddField("ERROR", "Current Channel is not a lobby!");
                await ReplyAsync("", false, embed.Build());
                return;
            }

            if (lobby.UserLimit != lobby.Users.Count && !lobby.IsPickingTeams)
            {
                embed.AddField("ERROR", "Lobby is not full!!");
                await ReplyAsync("", false, embed.Build());
                return;
            }
            if (!lobby.Users.Contains(user.Id))
            {
                embed.AddField("ERROR", "user is not in lobby.");
                await ReplyAsync("", false, embed.Build());
                return;
            }


            if (Context.User.Id == lobby.T1Captain)
            {
                if (lobby.Team1.Count == 0 || lobby.Team1 == null || lobby.Team2.Count >= lobby.Team1.Count)
                {
                    //Initialise with team1 always.
                    if (user.Id == lobby.T2Captain)
                    {
                        embed.AddField("ERROR", "User is a captain you coon!");
                        await ReplyAsync("", false, embed.Build());
                        return;
                    }

                    if (lobby.Team1.Count == 0)
                    {
                        lobby.Team1.Add(Context.User.Id);
                        lobby.Users.Remove(Context.User.Id);
                        lobby.Team2.Add(lobby.T2Captain);
                        lobby.Users.Remove(lobby.T2Captain);
                    }

                    lobby.Team1.Add(user.Id);
                    lobby.Users.Remove(user.Id);

                    var t1List = "";
                    var t2List = "";
                    var users = "";

                    var cap1 = await Context.Guild.GetUserAsync(lobby.T2Captain);
                    var cap2 = await Context.Guild.GetUserAsync(lobby.T1Captain);

                    foreach (var us in lobby.Team1)
                    {
                        var u = await Context.Client.GetUserAsync(us);
                        t1List += $"{u.Mention} ";
                    }
                    foreach (var us in lobby.Team2)
                    {
                        var u = await Context.Client.GetUserAsync(us);
                        t2List += $"{u.Mention} ";
                    }
                    foreach (var us in lobby.Users)
                    {
                        var u = await Context.Client.GetUserAsync(us);
                        users += $"{u.Mention} ";
                    }
                    embed.AddField($"{(user as IGuildUser).Nickname} Added",
                        $"[{lobby.Team1.Count}/{lobby.UserLimit / 2}]\n" +
                        $"Team1: {t1List}\n" +
                        $"Team2: {t2List}\n" +
                        $"\nCaptains: \n" +
                        $"1: {cap1.Mention}\n" +
                        $"2: {cap2.Mention}\n" +
                        $"Players Left: {users}");
                    await ReplyAsync("", false, embed.Build());
                    lobby.IsPickingTeams = true;
                    ServerList.Saveserver(server);
                    return;
                }

                if (lobby.Team1.Count > lobby.Team2.Count)
                {
                    embed.AddField("ERROR", "Team 2's turn to pick.");
                    await ReplyAsync("", false, embed.Build());
                    return;
                }

                if (lobby.Users.Count == 0 || lobby.Users == null)
                {
                    await Teams(server, lobby.Team1, lobby.Team2);
                    return;
                }

                embed.AddField("ERROR", "FUCK");
                await ReplyAsync("", false, embed.Build());
            }
            else if (Context.User.Id == lobby.T2Captain)
            {
                if (lobby.Team2.Count > lobby.Team1.Count || lobby.Team1.Count == 0 || lobby.Team1 == null)
                {
                    embed.AddField("ERROR", "Team 1's turn to pick.");
                    await ReplyAsync("", false, embed.Build());
                    return;
                }

                if (lobby.Team1.Count > lobby.Team2.Count)
                {
                    lobby.Team2.Add(user.Id);
                    lobby.Users.Remove(user.Id);
                    var t1List = "";
                    var t2List = "";
                    var users = "";

                    var cap1 = await Context.Guild.GetUserAsync(lobby.T2Captain);
                    var cap2 = await Context.Guild.GetUserAsync(lobby.T1Captain);
                    foreach (var us in lobby.Team1)
                    {
                        var u = await Context.Client.GetUserAsync(us);
                        t1List += $"{u.Mention} ";
                    }
                    foreach (var us in lobby.Team2)
                    {
                        var u = await Context.Client.GetUserAsync(us);
                        t2List += $"{u.Mention} ";
                    }
                    foreach (var us in lobby.Users)
                    {
                        var u = await Context.Client.GetUserAsync(us);
                        users += $"{u.Mention} ";
                    }
                    embed.AddField($"{(user as IGuildUser).Nickname} Added",
                        $"[{lobby.Team2.Count}/{lobby.UserLimit / 2}]\n" +
                        $"Team1: {t1List}\n" +
                        $"Team2: {t2List}\n" +
                        "\nCaptains: \n" +
                        $"1: {cap1.Mention}\n" +
                        $"2: {cap2.Mention}\n" +
                        $"Players Left: {users}");


                    await ReplyAsync("", false, embed.Build());
                    lobby.IsPickingTeams = true;
                    ServerList.Saveserver(server);
                }

                if (lobby.Users.Count == 0 || lobby.Users == null)
                {
                    await Teams(server, lobby.Team1, lobby.Team2);
                }

                embed.AddField("ERROR", "I dont think it's your turn to pick a player.....");
                await ReplyAsync("", false, embed.Build());

            }
            else
            {
                embed.AddField("ERROR", "Not A Captain!");
                await ReplyAsync("", false, embed.Build());
            }
        }

        [Command("Join")]
        [Summary("Join")]
        [Remarks("Join the current queue")]
        public async Task Join()
        {
            var server = ServerList.Load(Context.Guild);
            var embed = new EmbedBuilder();
            ServerList.Server.Q lobby;
            try
            {
                lobby = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            }
            catch
            {
                embed.AddField("ERROR", "Current Channel is not a lobby!");
                await ReplyAsync("", false, embed.Build());
                return;
            }

            if (lobby.IsPickingTeams)
            {
                embed.AddField("ERROR", "Teams are being picked, you cannot join the queue");
                await ReplyAsync("", false, embed.Build());
                return;
            }

            if (lobby.Users.Count < lobby.UserLimit)
            {
                if (!lobby.Users.Contains(Context.User.Id))
                {
                    lobby.Users.Add(Context.User.Id);
                    embed.AddField("Success", $"Added to the queue **[{lobby.Users.Count}/{lobby.UserLimit}]**");
                    await ReplyAsync("", false, embed.Build());
                    ServerList.Saveserver(server);
                }
                else
                {
                    embed.AddField("ERROR", "Already in queue.");
                    await ReplyAsync("", false, embed.Build());
                    return;
                }
                if (lobby.Users.Count >= lobby.UserLimit)
                {
                    lobby.Games++;
                    ServerList.Saveserver(server);
                    await FullQueue(server);
                }
            }
        }

        [Command("subfor")]
        [Summary("subfor <@user>")]
        [Remarks("replace the given user in the queue")]
        public async Task Sub(IUser user)
        {
            var server = ServerList.Load(Context.Guild);
            var embed = new EmbedBuilder();
            var queue = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            if (queue != null)
            {
                if (queue.Users.Contains(Context.User.Id))
                {
                    await ReplyAsync("Hey... youre already queued you coon.");
                    return;
                }
                if (queue.Users.Contains(user.Id))
                {
                    queue.Users.Remove(user.Id);

                    queue.Users.Add(Context.User.Id);
                    embed.AddField("Success", $"{user.Mention} has been replaced by {Context.User.Mention}\n" +
                                              $"**[{queue.Users.Count}/{queue.UserLimit}]**");
                }
                else
                {
                    embed.AddField("ERROR", $"{user.Mention} is not queued\n" +
                                            $"**[{queue.Users.Count}/{queue.UserLimit}]**");
                }


                ServerList.Saveserver(server);
                await ReplyAsync("", false, embed.Build());
            }
            else
            {
                await ReplyAsync("Error: No queue? or something... ask passive idk");
            }
        }

        [Command("Leave")]
        [Summary("Leave")]
        [Remarks("Leave the current queue")]
        public async Task Leave()
        {
            var server = ServerList.Load(Context.Guild);
            var embed = new EmbedBuilder();
            try
            {
                var queue = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);

                if (queue.IsPickingTeams)
                {
                    embed.AddField("ERROR",
                        "Teams are being picked, you cannot leave the queue. You may only be subbed.");
                    await ReplyAsync("", false, embed.Build());
                    return;
                }


                queue.Users.Remove(Context.User.Id);
                embed.AddField("Success", "You have been removed from the queue.\n" +
                                          $"**[{queue.Users.Count}/{queue.UserLimit}]**");

                ServerList.Saveserver(server);
                await ReplyAsync("", false, embed.Build());
            }
            catch
            {
                await ReplyAsync("Not Queued?");
            }
        }


        public async Task Teams(ServerList.Server server, List<ulong> team1, List<ulong> team2)
        {
            var team1Userlist = new List<IUser>();
            var t1 = "";
            foreach (var user in team1)
            {
                var u = await Context.Guild.GetUserAsync(user);
                team1Userlist.Add(u);
                t1 += $"{u.Mention} ";
            }

            var team2Userlist = new List<IUser>();
            var t2 = "";
            foreach (var user in team2)
            {
                var u = await Context.Guild.GetUserAsync(user);
                team2Userlist.Add(u);
                t2 += $"{u.Mention} ";
            }
            
            



            var host = await Context.Guild.GetUserAsync(team1[0]);

            //ADD GAME SAVING

            var currentqueue = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);

                if (currentqueue.Maps.Count > 0 && currentqueue.Maps != null)
                {
                    var rnd = new Random().Next(0, currentqueue.Maps.Count);
                    await ReplyAsync("**GAME ON**\n" +
                                     $"Team1: {t1}\n" +
                                     $"Team2: {t2}\n\n" +
                                     $"Random Map: {currentqueue.Maps[rnd]}");
                }
                else
                {
                    await ReplyAsync("**GAME ON**\n" +
                                     $"Team1: {t1}\n" +
                                     $"Team2: {t2}");
                }


            currentqueue.Users = new List<ulong>();
            currentqueue.Team1 = new List<ulong>();
            currentqueue.Team2 = new List<ulong>();
            currentqueue.T1Captain = 0;
            currentqueue.T2Captain = 0;
            currentqueue.IsPickingTeams = false;

            var newgame = new ServerList.Server.PreviouMatches
            {
                GameNumber = currentqueue.Games,
                LobbyId = Context.Channel.Id,
                Team1 = team1,
                Team2 = team2
            };
            server.Gamelist.Add(newgame);

            await Announce(currentqueue, host, currentqueue.ChannelGametype, team1Userlist, team2Userlist);


            ServerList.Saveserver(server);
        }

        public async Task FullQueue(ServerList.Server server)
        {
            try
            {
                var embed = new EmbedBuilder();
                var userlist = new List<ServerList.Server.User>();
                var currentqueue = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);

                foreach (var user in currentqueue.Users)
                {
                    var us = server.UserList.FirstOrDefault(x => x.UserId == user);
                    userlist.Add(us);
                }

                //order list by User Points
                if (currentqueue.Captains)
                {
                    var rnd = new Random();
                    var captains = Enumerable.Range(0, currentqueue.Users.Count).OrderBy(x => rnd.Next()).Take(2)
                        .ToList();
                    var cap1 = await Context.Guild.GetUserAsync(currentqueue.Users[captains[0]]);
                    var cap2 = await Context.Guild.GetUserAsync(currentqueue.Users[captains[1]]);

                    var players = "";
                    foreach (var user in currentqueue.Users)
                    {
                        var u = await Context.Guild.GetUserAsync(user);
                        if (u != cap1 && u != cap2)
                            players += $"{u.Mention} ";
                    }

                    await ReplyAsync($"**Team 1 Captain:** {cap1.Mention}\n" +
                                     $"**Team 2 Captain:** {cap2.Mention}\n" +
                                     "**Select Your Teams using `=pick <@user>`**\n" +
                                     "**Captain 1 Always Picks First**\n" +
                                     "**Players:**\n" +
                                     $"{players}");

                    currentqueue.T1Captain = cap1.Id;
                    currentqueue.T2Captain = cap2.Id;
                    currentqueue.Team1 = new List<ulong>();
                    currentqueue.Team2 = new List<ulong>();
                    currentqueue.IsPickingTeams = true;
                    ServerList.Saveserver(server);
                    return;
                }


                var sortedlist = userlist.OrderBy(x => x.Points).Reverse().ToList();
                var team1 = new List<ServerList.Server.User>();
                var team2 = new List<ServerList.Server.User>();
                foreach (var user in sortedlist)
                    if (team1.Count > team2.Count)
                        team2.Add(user);
                    else
                        team1.Add(user);


                var t1Desc = "";
                var t1Sum = 0;
                var t1Users = new List<IUser>();
                var t1Ulong = new List<ulong>();
                foreach (var user in team1)
                {
                    t1Desc += $"{user.Username} - {user.Points}\n";
                    t1Users.Add(Context.Guild.GetUserAsync(user.UserId).Result);
                    t1Ulong.Add(user.UserId);
                    t1Sum = t1Sum + user.Points;
                }

                var t2Desc = "";
                var t2Sum = 0;
                var t2Users = new List<IUser>();
                var t2Ulong = new List<ulong>();
                foreach (var user in team2)
                {
                    t2Desc += $"{user.Username} - {user.Points}\n";
                    t2Users.Add(Context.Guild.GetUserAsync(user.UserId).Result);
                    t2Ulong.Add(user.UserId);
                    t2Sum = t2Sum + user.Points;
                }


                embed.Title = $"Match #{currentqueue.Games}";
                embed.AddField($"Team 1 - {t1Sum}", $"{t1Desc}");
                embed.AddField($"Team 2 - {t2Sum}", $"{t2Desc}");

                if (currentqueue.ChannelGametype == null)
                    currentqueue.ChannelGametype = "Unknown";
                embed.AddField($"Match Info", $"{currentqueue.ChannelGametype}");

                try
                {
                    var r = new Random().Next(0, currentqueue.Maps.Count);
                    var randmap = currentqueue.Maps[r];
                    embed.AddField("Random Map", $"{randmap}");
                }
                catch
                {
                    //
                }


                await ReplyAsync("", false, embed.Build());
                currentqueue.Users = new List<ulong>();
                var newgame = new ServerList.Server.PreviouMatches
                {
                    GameNumber = currentqueue.Games,
                    LobbyId = Context.Channel.Id,
                    Team1 = t1Ulong,
                    Team2 = t2Ulong
                };
                server.Gamelist.Add(newgame);

                ServerList.Saveserver(server);
                var random = new Random().Next(0, sortedlist.Count);
                var gamehost = await Context.Guild.GetUserAsync(sortedlist[random].UserId);
                await Announce(currentqueue, gamehost, currentqueue.ChannelGametype, t1Users, t2Users);
            }
            catch (Exception e)
            {
                await ReplyAsync(e.ToString());
            }
        }

        public async Task Announce(ServerList.Server.Q lobby, IGuildUser gamehost, string matchdescription,
            List<IUser> team1,
            List<IUser> team2)
        {
            var server = ServerList.Load(Context.Guild);
            IMessageChannel channel;
            try
            {
                channel = await Context.Guild.GetChannelAsync(server.AnnouncementsChannel) as IMessageChannel;
            }
            catch
            {
                channel = Context.Channel;
            }

            var lobbychannel = await Context.Client.GetChannelAsync(lobby.ChannelId);
            var announcement = "**__Game Has Started__**\n" +
                               "**Lobby:** \n" +
                               $"{lobbychannel.Name} - Match #{lobby.Games}\n" +
                               $"**Selected Host:** \n" +
                               $"{gamehost.Mention}\n" +
                               $"**Match Settings:**\n" +
                               $"{matchdescription}\n" +
                               $"**Team 1:** [{string.Join(" ", team1.Select(x => x.Mention))}]\n" +
                               $"**Team 2**: [{string.Join(" ", team2.Select(x => x.Mention))}]\n" +
                               $"When the game finishes, type `=game {lobbychannel.Name} {lobby.Games} <team1 or team2>`\n" +
                               $"This will modify each team's points respectively.";

            await channel.SendMessageAsync(announcement);
        }
    }
}