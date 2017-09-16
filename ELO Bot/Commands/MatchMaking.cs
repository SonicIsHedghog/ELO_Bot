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
        [Command("Queue")]
        [Alias("q")]
        [Summary("Queue")]
        [Remarks("Display the current queue")]
        public async Task Queue()
        {
            var server = ServerList.Load(Context.Guild);
            var embed = new EmbedBuilder();
            try
            {
                var lobby = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
                //get the current queue
                if (lobby.IsPickingTeams)
                {
                    //if teams are currently being picked, display teams in the queue rather than just the queue.
                    var t1List = "";
                    var t2List = "";
                    var users = "";

                    var cap1 = await Context.Guild.GetUserAsync(lobby.T2Captain);
                    var cap2 = await Context.Guild.GetUserAsync(lobby.T1Captain);

                    //create a list of users in team 1 2 and users left
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
                    //empty lobby
                    embed.AddField($"{Context.Channel.Name} Queue **[0/{lobby.UserLimit}]** #{lobby.Games + 1}",
                        $"Empty");
                }
                else
                {
                    //get all users in the current lobby and find their user registrations for the server.
                    var list = "";
                    foreach (var user in lobby.Users)
                    {
                        var subject = server.UserList.FirstOrDefault(x => x.UserId == user);
                        list += $"{subject.Username} - {subject.Points}\n";
                        //create a list of usernames and their points
                    }

                    embed.AddField(
                        $"{Context.Channel.Name} Queue **[{lobby.Users.Count}/{lobby.UserLimit}]** #{lobby.Games + 1}",
                        $"{list}");
                }
                embed.AddField("Join/Leave", "`=Join` To Join the queue\n" +
                                             "`=Leave` To Leave the queue\n" +
                                             "`=subfor <@user>` Replace a user");
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
            //creating general info about the current lobby.
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
        [Alias("p")]
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
                //ensure that the current channel is a lobby
                embed.AddField("ERROR", "Current Channel is not a lobby!");
                await ReplyAsync("", false, embed.Build());
                return;
            }

            if (lobby.UserLimit != lobby.Users.Count && !lobby.IsPickingTeams)
            {
                //if the lobby is not full or teams are not being picked.
                embed.AddField("ERROR", "Lobby is not full!!");
                await ReplyAsync("", false, embed.Build());
                return;
            }
            if (!lobby.Users.Contains(user.Id))
            {
                //make sure the user is actually in the lobby.
                embed.AddField("ERROR", "user is not in lobby/user has already been picked.");
                await ReplyAsync("", false, embed.Build());
                return;
            }


            if (Context.User.Id == lobby.T1Captain)
            {
                //check if team 1 players have been picked yet
                //is team2's player count is greater than or equal to team 1's user count (should always be equal however)
                if (lobby.Team1.Count == 0 || lobby.Team1 == null || lobby.Team2.Count >= lobby.Team1.Count)
                {
                    //make sure that users do not pick the other team's captain
                    if (user.Id == lobby.T2Captain)
                    {
                        embed.AddField("ERROR", "User is a captain you coon!");
                        await ReplyAsync("", false, embed.Build());
                        return;
                    }

                    //for initialiseing teams make sure team2's captain is added to their team
                    //also make sure both captains are added to the correct teams.
                    if (lobby.Team1.Count == 0)
                    {
                        lobby.Team1.Add(Context.User.Id);
                        lobby.Users.Remove(Context.User.Id);
                        lobby.Team2.Add(lobby.T2Captain);
                        lobby.Users.Remove(lobby.T2Captain);
                    }


                    //add the specified user to the team
                    lobby.Team1.Add(user.Id);
                    lobby.Users.Remove(user.Id);

                    if (lobby.Users.Count == 1)
                    {
                        var u = lobby.Users.First();
                        lobby.Team2.Add(u);
                        lobby.Users.Remove(u);
                    }

                    var t1List = "";
                    var t2List = "";
                    var users = "";

                    var cap1 = await Context.Guild.GetUserAsync(lobby.T2Captain);
                    var cap2 = await Context.Guild.GetUserAsync(lobby.T1Captain);

                    //create a list of users in team1, team2 and users left.
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

                    //if teams have both been filled finish the queue
                    if (lobby.Users.Count == 0 || lobby.Users == null)
                        await Teams(server, lobby.Team1, lobby.Team2);
                    return;
                }

                //make sure teams are picked in turns.
                if (lobby.Team1.Count > lobby.Team2.Count)
                {
                    embed.AddField("ERROR", "Team 2's turn to pick.");
                    await ReplyAsync("", false, embed.Build());
                    return;
                }


                embed.AddField("ERROR", "FUCK tell Passive to fix something...");
                await ReplyAsync("", false, embed.Build());
            }
            else if (Context.User.Id == lobby.T2Captain)
            {
                //make sure team one has picked before starting team2
                if (lobby.Team2.Count > lobby.Team1.Count || lobby.Team1.Count == 0 || lobby.Team1 == null)
                {
                    embed.AddField("ERROR", "Team 1's turn to pick.");
                    await ReplyAsync("", false, embed.Build());
                    return;
                }

                //add specified user to team 2
                if (lobby.Team1.Count > lobby.Team2.Count)
                {
                    lobby.Team2.Add(user.Id);
                    lobby.Users.Remove(user.Id);
                    var t1List = "";
                    var t2List = "";
                    var users = "";

                    var cap1 = await Context.Guild.GetUserAsync(lobby.T1Captain);
                    var cap2 = await Context.Guild.GetUserAsync(lobby.T2Captain);
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

                    if (lobby.Users.Count == 0 || lobby.Users == null)
                        await Teams(server, lobby.Team1, lobby.Team2);
                    return;
                }


                //idk this should never happen.
                embed.AddField("ERROR", "I dont think it's your turn to pick a player.....");
                await ReplyAsync("", false, embed.Build());
            }
            else
            {
                //make sure that only captains can choose players.
                embed.AddField("ERROR", "Not A Captain!");
                await ReplyAsync("", false, embed.Build());
            }
        }

        [Command("Join")]
        [Summary("Join")]
        [Alias("j")]
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

            //users can only join the queue when teams are not being picked.
            if (lobby.IsPickingTeams)
            {
                embed.AddField("ERROR", "Teams are being picked, you cannot join the queue");
                await ReplyAsync("", false, embed.Build());
                return;
            }

            //make sure that the users never reach the userlimit.
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
                    //if lobby is full increment the game count by 1.
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
            //get the current lobbies queue.
            if (queue != null)
            {
                if (queue.Users.Contains(Context.User.Id))
                {
                    //ensure that you cannot replace a player if you are already in the queue.
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


        [Command("replace")]
        [Summary("replace <@user>")]
        [Remarks("replace the specified user in the previously chosen game")]
        public async Task Replace(IUser user)
        {
            var server = ServerList.Load(Context.Guild);
            var embed = new EmbedBuilder();
            var queue = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            var oldgame =
                server.Gamelist.FirstOrDefault(x => x.LobbyId == Context.Channel.Id && x.GameNumber == queue.Games);
            if (oldgame != null)
            {
                //check thay you are not already in the old game.
                if (oldgame.Team1.Contains(Context.User.Id) || oldgame.Team2.Contains(Context.User.Id))
                {
                    await ReplyAsync(
                        "You are already in a team for the previous game. Only users that were'nt in this game can replace others.");
                    return;
                }


                if (oldgame.Team1.Contains(user.Id))
                {
                    //remove specified user and replace with new user.
                    oldgame.Team1.Remove(user.Id);
                    oldgame.Team1.Add(Context.User.Id);

                    await ReplyAsync(
                        $"Game #{oldgame.GameNumber} Team 1: {user.Mention} has been replaced by {Context.User.Mention}");
                }
                if (oldgame.Team2.Contains(user.Id))
                {
                    oldgame.Team2.Remove(user.Id);
                    oldgame.Team2.Add(Context.User.Id);

                    await ReplyAsync(
                        $"Game #{oldgame.GameNumber} Team 2: {user.Mention} has been replaced by {Context.User.Mention}");
                }

                if (server.AnnouncementsChannel != 0)
                {
                    var t1Mention = new List<IUser>();
                    var t2Mention = new List<IUser>();

                    foreach (var u in oldgame.Team1)
                    {
                        var use = await Context.Guild.GetUserAsync(u);
                        t1Mention.Add(use);
                    }
                    foreach (var u in oldgame.Team2)
                    {
                        var use = await Context.Guild.GetUserAsync(u);
                        t2Mention.Add(use);
                    }
                    var announcement = "**__Game Has Been Updated__**\n" +
                                       "**Lobby:** \n" +
                                       $"{Context.Channel.Name} - Match #{oldgame.GameNumber}\n" +
                                       $"**Team 1:** [{string.Join(" ", t1Mention.Select(x => x.Mention).ToList())}]\n" +
                                       $"**Team 2**: [{string.Join(" ", t2Mention.Select(x => x.Mention).ToList())}]\n" +
                                       $"When the game finishes, type `=game {Context.Channel.Name} {oldgame.GameNumber} <team1 or team2>`\n" +
                                       "This will modify each team's points respectively.";

                    try
                    {
                        var channel = await Context.Guild.GetChannelAsync(server.AnnouncementsChannel);
                        await (channel as IMessageChannel).SendMessageAsync(announcement);
                    }
                    catch
                    {
                        await ReplyAsync(announcement);
                    }
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

        [Command("Maps")]
        [Summary("Maps")]
        [Remarks("List Maps")]
        public async Task Maps()
        {
            var embed = new EmbedBuilder();
            var server = ServerList.Load(Context.Guild);
            //load the current server
            var lobby = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            //try to output the current lobby
            foreach (var map in lobby.Maps)
                embed.Description += $"{map}\n";
            //adds each map in the list to the embed

            await ReplyAsync("", false, embed.Build());
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

            var currentqueue = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);

            var host = await Context.Guild.GetUserAsync(currentqueue.T1Captain);

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
                var currentqueue = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);

                var userlist = currentqueue.Users.Select(user => server.UserList.FirstOrDefault(x => x.UserId == user))
                    .ToList();

                //order list by User Points
                if (currentqueue.Captains)
                {
                    //randomly select the captains on each team.
                    var rnd = new Random();

                    IUser cap1;
                    IUser cap2;
                    var capranks = new List<ulong>();
                    foreach (var user in currentqueue.Users)
                    {
                        var u = userlist.FirstOrDefault(x => x.UserId == user);
                        if (u.Points > 5)
                        {
                            capranks.Add(user);
                        }
                    }

                    if (capranks.Count >= 2)
                    {
                        var cap = Enumerable.Range(0, capranks.Count).OrderBy(x => rnd.Next()).Take(2)
                            .ToList();
                        cap1 = await Context.Guild.GetUserAsync(capranks[cap[0]]);
                        cap2 = await Context.Guild.GetUserAsync(capranks[cap[1]]);
                    }
                    else
                    {
                        var captains = Enumerable.Range(0, currentqueue.Users.Count).OrderBy(x => rnd.Next()).Take(2)
                        .ToList();
                        cap1 = await Context.Guild.GetUserAsync(currentqueue.Users[captains[0]]);
                        cap2 = await Context.Guild.GetUserAsync(currentqueue.Users[captains[1]]);
                    }
                    

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
                    //make sure that all players are mentioned to notify them that a game has begun

                    currentqueue.T1Captain = cap1.Id;
                    currentqueue.T2Captain = cap2.Id;
                    currentqueue.Team1 = new List<ulong>();
                    currentqueue.Team2 = new List<ulong>();
                    currentqueue.IsPickingTeams = true;
                    ServerList.Saveserver(server);
                    return;
                }


                //automatically select teams evenly based on points
                var sortedlist = userlist.OrderBy(x => x.Points).Reverse().ToList();
                var team1 = new List<ServerList.Server.User>();
                var team2 = new List<ServerList.Server.User>();
                foreach (var user in sortedlist)
                    if (team1.Count > team2.Count)
                        team2.Add(user);
                    else
                        team1.Add(user);

                //creating the info for each team
                var t1Desc = "";
                var t1Sum = team1.Sum(x => x.Points);
                var t1Users = new List<IUser>();
                foreach (var user in team1)
                {
                    t1Desc += $"{user.Username} - {user.Points}\n";
                    t1Users.Add(Context.Guild.GetUserAsync(user.UserId).Result);
                }

                var t2Desc = "";
                var t2Sum = team1.Sum(x => x.Points);
                var t2Users = new List<IUser>();
                foreach (var user in team2)
                {
                    t2Desc += $"{user.Username} - {user.Points}\n";
                    t2Users.Add(Context.Guild.GetUserAsync(user.UserId).Result);
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
                    Team1 = t1Users.Select(x => x.Id).ToList(),
                    Team2 = t2Users.Select(x => x.Id).ToList()
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