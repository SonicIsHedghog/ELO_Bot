using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord;

namespace ELO_Bot.Commands
{
    public class Match10 : ModuleBase
    {
        [Command("Maps")]
        [Summary("Maps")]
        [Remarks("List Maps")]
        public async Task Maps()
        {
            var embed = new EmbedBuilder();
            var server = ServerList.Load(Context.Guild);
            foreach (var map in server.Maps)
            {
                embed.Description += $"{map}\n";
            }

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
                    embed.AddField($"{Context.Channel.Name} Queue", $"Empty");
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

                    embed.AddField($"{Context.Channel.Name} Queue", $"{list}");
                }
                
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
        [CheckRegistered]
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

            if (lobby.Users.Count < 10)
            {
                if (!lobby.Users.Contains(Context.User.Id))
                {
                    lobby.Users.Add(Context.User.Id);
                    embed.AddField("Success", $"Added to the queue **[{lobby.Users.Count}/10]**");
                    await ReplyAsync("", false, embed.Build());
                    ServerList.Saveserver(server);
                }
                else
                {
                    embed.AddField("ERROR", "Already in queue.");
                    await ReplyAsync("", false, embed.Build());
                    return;
                }
                if (lobby.Users.Count >= 10)
                {
                    await FullQueue(server);
                }   
            }           
        }

        [Command("Leave")]
        [Summary("Leave")]
        [Remarks("Leave the current queue")]
        [CheckRegistered]
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
                                              $"**[{queue.Users.Count}/10]**");

                    ServerList.Saveserver(server);
                    await ReplyAsync("", false, embed.Build());
                    return;
                }
                
                await ReplyAsync($"Removed From Queue **[{server.Queue.Count}/10]**");
            }
            catch
            {
                await ReplyAsync("Not Queued?");
            }
        }

        public async Task FullQueue(ServerList.Server server)
        {
            await ReplyAsync("Queue Is Full!");
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
            {
                if (team1.Count > team2.Count)
                {
                    team2.Add(user);
                }
                else
                {
                    team1.Add(user);
                }

            }

            var t1Desc = "";
            var t1Sum = 0;
            foreach (var user in team1)
            {
                t1Desc += $"{user.Username} - {user.Points}\n";
                t1Sum = t1Sum + user.Points;
            }
            var t2Desc = "";
            var t2Sum = 0;
            foreach (var user in team2)
            {
                t2Desc += $"{user.Username} - {user.Points}\n";
                t2Sum = t2Sum + user.Points;
            }
            embed.AddField($"Team 1 - {t1Sum}", $"{t1Desc}");
            embed.AddField($"Team 2 - {t2Sum}", $"{t2Desc}");

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
            ServerList.Saveserver(server);
        }
    }
}
