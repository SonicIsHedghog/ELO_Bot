using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using ELO_Bot.Preconditions;

namespace ELO_Bot.Commands.Admin
{
    /// <summary>
    ///     checks ensure that blacklisted commands are not run
    ///     and that these commands are only used by an admin
    /// </summary>
    [CheckBlacklist]
    [CheckAdmin]
    public class Lobby : InteractiveBase
    {
        /// <summary>
        ///     creates a lobby
        ///     requires the user to provide the following:
        ///     Users Per Game
        ///     If teams are automatically chosen or chosen by team captains
        ///     Lobby Description
        /// </summary>
        /// <param name="lolsdvdv">ignored text, previously used in a different context</param>
        /// <returns></returns>
        [Command("Createlobby", RunMode = RunMode.Async)]
        [Summary("Createlobby")]
        [Remarks("Initialise a lobby in the current channel")]
        public async Task Createlobby([Remainder] string lolsdvdv = null)
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);

            var embed = new EmbedBuilder();

            var lobbyexists = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            if (lobbyexists == null)
            {
                await ReplyAsync(
                    "Please reply with a number for the amount of players you want for the lobby, ie. 10 gives two teams of 5");
                var n1 = await NextMessageAsync(timeout: TimeSpan.FromMinutes(1d));
                if (int.TryParse(n1.Content, out var i))
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


                            var ser = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
                            ser.Queue.Add(new Servers.Server.Q
                            {
                                ChannelId = Context.Channel.Id,
                                Users = new List<ulong>(),
                                UserLimit = i,
                                ChannelGametype = n3.Content,
                                Captains = captains
                            });


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
                else
                    await ReplyAsync("ERROR: Not an integer");
            }
            else
            {
                await ReplyAsync(
                    $"ERROR: Current channel is already a lobby OR Command timed out. {Context.User.Mention}");
            }
        }

        /// <summary>
        ///     removes the current channel from being used as a lobby if applicable
        /// </summary>
        /// <returns></returns>
        [Command("RemoveLobby")]
        [Summary("RemoveLobby")]
        [Remarks("Remove A Lobby")]
        public async Task ClearLobby()
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            var q = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            server.Queue.Remove(q);

            var removegames = server.Gamelist.Where(game => game.LobbyId == Context.Channel.Id).ToList();
            foreach (var game in removegames)
                if (server.Gamelist.Contains(game))
                    server.Gamelist.Remove(game);

            await ReplyAsync($"{Context.Channel.Name} is no longer a lobby!\n" +
                             "Previous games that took place in this lobby have been cleared from history.");
        }

        /// <summary>
        ///     removes all players form the current queue
        /// </summary>
        /// <returns></returns>
        [Command("ClearQueue")]
        [Summary("ClearQueue")]
        [Remarks("Clear All Players from The Queue")]
        public async Task ClearQueue()
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
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

            await ReplyAsync($"{Context.Channel.Name}'s queue has been cleared!");
        }

        /// <summary>
        ///     adds a list of maps to the current lobby
        /// </summary>
        /// <param name="mapName">list of maps ie. map1 map2 map3...</param>
        /// <returns></returns>
        [Command("AddMap")]
        [Summary("AddMap <Map_0> <Map_1>")]
        [Remarks("Add A Map")]
        public async Task AddMap(params string[] mapName)
        {
            var embed = new EmbedBuilder();
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
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
        }

        /// <summary>
        ///     remove a map from the maps list
        /// </summary>
        /// <param name="mapName">the name of the map you want to delete</param>
        /// <returns></returns>
        [Command("DelMap")]
        [Summary("DelMap <MapName>")]
        [Remarks("Delete A Map")]
        public async Task DeleteMap(string mapName)
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            var lobby = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);

            if (lobby == null)
            {
                await ReplyAsync("Current channel is not a lobby!");
                return;
            }

            if (lobby.Maps.Select(x => x.ToLower()).Contains(mapName.ToLower()))
            {
                lobby.Maps.Remove(mapName);
                await ReplyAsync($"Map Removed {mapName}");
            }
            else
            {
                await ReplyAsync($"Map doesnt exist {mapName}");
            }
        }

        /// <summary>
        ///     rather than adding maps, set the entire list of maps for the current channel
        /// </summary>
        /// <param name="mapName">list of maps ie. map1 map2 map3...</param>
        /// <returns></returns>
        [Command("SetMaps")]
        [Summary("SetMaps <Map_0> <Map_1>")]
        [Remarks("Set all maps for the current lobby")]
        public async Task SetMaps(params string[] mapName)
        {
            var embed = new EmbedBuilder();
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            var lobby = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            if (lobby == null)
            {
                await ReplyAsync("Current channel is not a lobby!");
                return;
            }

            embed.Title = $"{Context.Channel.Name}";

            foreach (var map in mapName)
                embed.Description += $"{map}\n";

            lobby.Maps = mapName.ToList();

            await ReplyAsync("", false, embed.Build());
        }

        /// <summary>
        ///     clears all maps for the current lobby
        /// </summary>
        /// <returns></returns>
        [Command("ClearMaps")]
        [Summary("ClearMaps")]
        [Remarks("Clear all maps for the current lobby")]
        public async Task ClearMap()
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            var lobby = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);

            if (lobby == null)
            {
                await ReplyAsync("Current channel is not a lobby!");
                return;
            }

            lobby.Maps = new List<string>();
            await ReplyAsync("Maps Cleared.");
        }
    }
}