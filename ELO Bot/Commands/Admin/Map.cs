using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace ELO_Bot.Commands.Admin
{
    [CheckAdmin]
    public class Map : ModuleBase
    {
        [Command("AddMap")]
        [Summary("AddMap <Map_0> <Map_1>")]
        [Remarks("Add A Map")]
        public async Task AddMap(params string[] mapName)
        {
            var server = ServerList.Load(Context.Guild);
            var lobby = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            foreach (var map in mapName)
            {
                if (!lobby.Maps.Contains(map))
                {
                    lobby.Maps.Add(map);
                    await ReplyAsync($"Map added {map}");
                }
                else
                {
                    await ReplyAsync($"Map Already Exists {map}");
                }
            }


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
            lobby.Maps = new System.Collections.Generic.List<string>();
            ServerList.Saveserver(server);
            await ReplyAsync("Maps Cleared.");
        }
    }
}