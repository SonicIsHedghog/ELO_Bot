using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using ELO_Bot.PreConditions;

namespace ELO_Bot.Commands.Admin
{
    [CheckAdmin]
    public class Lobby : ModuleBase
    {

        [Ratelimit(1, 30d, Measure.Seconds)]
        [Command("CreateLobby")]
        [Summary("CreateLobby <userlimit> <true = captains, false = automatic teams> <lobby message>")]
        [Remarks("Turn The current channel into a lobby")]
        public async Task LobbyCreate(int userlimit, bool captains, [Remainder] string lobbyMessage = null)
        {
            try
            {
                var server = ServerList.Load(Context.Guild);
                var embed = new EmbedBuilder();
                try
                {
                    if (userlimit % 2 != 0)
                    {
                        embed.AddField("ERROR", "Userlimit must be even ie. 10 for two teams of 5");
                        await ReplyAsync("", false, embed.Build());
                        return;
                    }

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


        [Ratelimit(1, 30d, Measure.Seconds)]
        [Command("RemoveLobby")]
        [Summary("RemoveLobby")]
        [Remarks("Remove A Lobby")]
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


        [Ratelimit(1, 30d, Measure.Seconds)]
        [Command("Clear")]
        [Summary("Clear")]
        [Remarks("Clear The Queue")]
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

        [Command("AddMap")]
        [Summary("AddMap <Map_0> <Map_1>")]
        [Remarks("Add A Map")]
        public async Task AddMap(params string[] mapName)
        {
            var embed = new EmbedBuilder();
            var server = ServerList.Load(Context.Guild);
            var lobby = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            foreach (var map in mapName)
                if (!lobby.Maps.Contains(map))
                {
                    lobby.Maps.Add(map);
                    embed.Description+= $"Map added {map}\n";
                }
                else
                {
                    embed.Description += $"Map Already Exists {map}\n";
                }

            await ReplyAsync("", false, embed.Build());
            ServerList.Saveserver(server);
        }

        [Command("DelMap")]
        [Summary("DelMap <MapName>")]
        [Remarks("Delete A Map")]
        public async Task DeleteMap(string mapName)
        {
            var server = ServerList.Load(Context.Guild);
            var lobby = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            if (lobby.Maps.Contains(mapName))
            {
                lobby.Maps.Remove(mapName);
                await ReplyAsync($"Map Removed {mapName}");
                ServerList.Saveserver(server);
            }
            else
            {
                await ReplyAsync($"Map doesnt exist {mapName}");
            }
        }

        [Command("ClearMaps")]
        [Summary("ClearMaps")]
        [Remarks("Clear all maps for the current lobby")]
        public async Task ClearMap()
        {
            var server = ServerList.Load(Context.Guild);
            var lobby = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            lobby.Maps = new List<string>();
            ServerList.Saveserver(server);
            await ReplyAsync("Maps Cleared.");
        }
    }
}