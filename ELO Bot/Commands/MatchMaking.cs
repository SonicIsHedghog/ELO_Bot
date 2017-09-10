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
            foreach (var map in server.Maps)
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
                var lobbyexists = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
                if (lobbyexists.Users.Count == 0)
                {
                    //empty
                    embed.AddField($"{Context.Channel.Name} Queue **[0/{lobbyexists.UserLimit}]**", $"Empty");
                }
                else
                {
                    //make list
                    var list = "";
                    foreach (var user in lobbyexists.Users)
                    {
                        var subject = server.UserList.FirstOrDefault(x => x.UserId == user);
                        list += $"{subject.Username} - {subject.Points}\n";
                    }

                    embed.AddField(
                        $"{Context.Channel.Name} Queue **[{lobbyexists.Users.Count}/{lobbyexists.UserLimit}]**",
                        $"{list}");
                }
                if (lobbyexists.ChannelGametype == null)
                    lobbyexists.ChannelGametype = "Unknown";

                embed.AddField("Lobby Info", $"**Player Limit:**\n" +
                                             $"{lobbyexists.UserLimit}\n" +
                                             $"**Game Number:**\n" +
                                             $"{lobbyexists.Games + 1}\n" +
                                             $"**Gamemode Description:**\n" +
                                             $"{lobbyexists.ChannelGametype}");
            }
            catch
            {
                embed.AddField("Error", "The current channel is not a lobby, there is no queue here.");
            }

            embed.AddField("Join/Leave", "`=Join` To Join the queue\n" +
                                         "`=Leave` To Leave the queue");


            await ReplyAsync("", false, embed.Build());
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

            if (lobby.Users.Count < lobby.UserLimit)
            {
                //if (!lobby.Users.Contains(Context.User.Id))
                {
                    lobby.Users.Add(Context.User.Id);
                    embed.AddField("Success", $"Added to the queue **[{lobby.Users.Count}/{lobby.UserLimit}]**");
                    await ReplyAsync("", false, embed.Build());
                    ServerList.Saveserver(server);
                }
                //else
                //{
                //    embed.AddField("ERROR", "Already in queue.");
                //    await ReplyAsync("", false, embed.Build());
                //    return;
                //}
                if (lobby.Users.Count >= lobby.UserLimit)
                {
                    lobby.Games++;
                    ServerList.Saveserver(server);
                    await FullQueue(server);
                }
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
                if (queue != null)
                {
                    queue.Users.Remove(Context.User.Id);
                    embed.AddField("Success", "You have been removed from the queue.\n" +
                                              $"**[{queue.Users.Count}/{queue.UserLimit}]**");

                    ServerList.Saveserver(server);
                    await ReplyAsync("", false, embed.Build());
                    return;
                }

                await ReplyAsync($"Removed From Queue **[{server.Queue.Count}/{queue.UserLimit}]**");
            }
            catch
            {
                await ReplyAsync("Not Queued?");
            }
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
                    var r = new Random().Next(0, server.Maps.Count);
                    var randmap = server.Maps[r];
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
            if (server.AnnouncementsChannel == 0)
                return;
            var channel = await Context.Guild.GetChannelAsync(server.AnnouncementsChannel);
            var lobbychannel = await Context.Client.GetChannelAsync(lobby.ChannelId);
            var announcement = "**__Game Has Started__**\n" +
                               "**Lobby:** \n" +
                               $"{lobbychannel.Name} - Match #{lobby.Games}\n" +
                               $"**Selected Host:** \n" +
                               $"{gamehost.Mention}\n" +
                               $"**Match Settings:**\n" +
                               $"{matchdescription}\n" +
                               $"**Team 1:** [{string.Join(" ", team1.Select(x => x.Mention))}]\n" +
                               $"**Team 2**: [{string.Join(" ", team2.Select(x => x.Mention))}]";
            await (channel as IMessageChannel).SendMessageAsync(announcement);
        }
    }
}