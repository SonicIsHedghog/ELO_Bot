using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ELO_Bot.Commands.Admin
{
    public class LobbyManagement : ModuleBase
    {
        [Command("CreateLobby")]
        [Summary("CreateLobby <userlimit> <lobby message>")]
        [Remarks("Turn The current channel into a lobby")]
        [CheckRegistered]
        public async Task LobbyCreate(int userlimit, bool captains, [Remainder] string lobbyMessage = null)
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
                            Users = new List<ulong>(),
                            UserLimit = userlimit,
                            ChannelGametype = lobbyMessage,
                            Captains = captains
                        });

                        if (lobbyMessage == null)
                            lobbyMessage = "Unknown";
                        embed.AddField("LOBBY CREATED", $"**Lobby Name:** \n{Context.Channel.Name}\n" +
                                                        $"**PlayerLimit:** \n{userlimit}\n" +
                                                        $"**Captains:** \n{captains}\n" +
                                                        $"**GameMode Info:**\n" +
                                                        $"{lobbyMessage}");
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
                        server.Queue = new List<ServerList.Server.Q> {newlobby};
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

            var removegames = server.Gamelist.Where(game => game.LobbyId == Context.Channel.Id).ToList();
            foreach (var game in removegames)
                if (server.Gamelist.Contains(game))
                    server.Gamelist.Remove(game);

            ServerList.Saveserver(server);
            await ReplyAsync($"{Context.Channel.Name} is no longer a lobby!\n" +
                             $"Previous games that took place in this lobby have been cleared from history.");
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
            q.IsPickingTeams = false;
            q.Team1 = new List<ulong>();
            q.Team2 = new List<ulong>();
            q.T1Captain = 0;
            q.T2Captain = 0;

            ServerList.Saveserver(server);
            await ReplyAsync($"{Context.Channel.Name}'s queue has been cleared!");
        }
    }
}