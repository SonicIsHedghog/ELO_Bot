using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace ELO_Bot.Commands.Admin
{
    [CheckAdmin]
    public class MapManagement : ModuleBase
    {
        [Command("AddMap")]
        [Summary("AddMap <MapName>")]
        [Remarks("Add A Map")]
        public async Task AddMap(string mapName)
        {
            var server = ServerList.Load(Context.Guild);
            var lobby = server.Queue.FirstOrDefault(x => x.ChannelId == Context.Channel.Id);
            if (!lobby.Maps.Contains(mapName))
            {
                lobby.Maps.Add(mapName);
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
    }
}