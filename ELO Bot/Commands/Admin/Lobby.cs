using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using ELO_Bot.PreConditions;

namespace ELO_Bot.Commands.Admin
{
    [CheckAdmin]
    public class Lobby : InteractiveBase
    {
        [Command("Createlobby", RunMode = RunMode.Async)]
        [Summary("Ctreatelobby")]
        [Remarks("Initialise a lobby in the current channel")]
        public async Task Createlobby()
        {
            var server = ServerList.Load(Context.Guild);

            var embed = new EmbedBuilder();

            var lobbyexists = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            if (lobbyexists == null)
            {
                await ReplyAsync(
                    "Please reply with a number for the amount of players you want for the lobby, ie. 10 gives two teams of 5");
                var n1 = await NextMessageAsync(timeout: TimeSpan.FromMinutes(1d));
                if (int.TryParse(n1.Content, out var i))
                {
                    if (i % 2 != 0 || i < 4)
                    {
                        await ReplyAsync("ERROR: Number must be even and greater than 4 (minimum 2v2)");
                    }
                    else
                    {
                        await ReplyAsync("Please reply:\n" +
                                         "`true` for captains to choose the players for each team\n" +
                                         "`false` for teams to automatically be chosen");
                        var n2 = await NextMessageAsync(timeout: TimeSpan.FromMinutes(1d));
                        if (bool.TryParse(n2.Content, out var captains))
                        {
                            await ReplyAsync("Please specify a description for this lobby:\n" +
                                             "ie. \"Ranked Gamemode, 5v5 ELITE Players Only!\"");
                            var n3 = await NextMessageAsync(timeout: TimeSpan.FromMinutes(1d));
                            


                            var ser = ServerList.Load(Context.Guild);
                            ser.Queue.Add(new ServerList.Server.Q
                            {
                                ChannelId = Context.Channel.Id,
                                Users = new List<ulong>(),
                                UserLimit = i,
                                ChannelGametype = n3.Content,
                                Captains = captains
                            });

                            ServerList.Saveserver(ser);

                            embed.AddField("LOBBY CREATED", $"**Lobby Name:** \n{Context.Channel.Name}\n" +
                                                            $"**PlayerLimit:** \n{i}\n" +
                                                            $"**Captains:** \n{captains}\n" +
                                                            "**GameMode Info:**\n" +
                                                            $"{n3.Content}");
                            await ReplyAsync("", false, embed.Build());
                        }
                        else
                        {
                            await ReplyAsync("ERROR: Invalid type specified.");
                        }


                    }
                }
                else
                {
                    await ReplyAsync("ERROR: Not an integer");
                }
            }
            else
            {
                await ReplyAsync(
                    $"ERROR: Current channel is already a lobby OR Command timed out. {Context.User.Mention}");
            }
        }

        
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
                             "Previous games that took place in this lobby have been cleared from history.");
        }

        
        [Command("Clear")]
        [Summary("Clear")]
        [Remarks("Clear The Queue")]
        public async Task ClearQueue()
        {
            var server = ServerList.Load(Context.Guild);
            var q = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);

            if (q == null)
            {
                await ReplyAsync("ERROR: Current Channel is not a lobby!");
                return;
            }

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
            if (lobby == null)
            {
                await ReplyAsync("Current channel is not a lobby!");
                return;
            }
                
            foreach (var map in mapName)
                if (!lobby.Maps.Contains(map))
                {
                    lobby.Maps.Add(map);
                    embed.Description += $"Map added {map}\n";
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

            if (lobby == null)
            {
                await ReplyAsync("Current channel is not a lobby!");
                return;
            }

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

            if (lobby == null)
            {
                await ReplyAsync("Current channel is not a lobby!");
                return;
            }

            lobby.Maps = new List<string>();
            ServerList.Saveserver(server);
            await ReplyAsync("Maps Cleared.");
        }
    }
}