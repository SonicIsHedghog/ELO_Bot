using System.Threading.Tasks;
using Discord.Commands;

namespace ELO_Bot.Commands.Admin
{
    public class MapManagement : ModuleBase
    {
        [Command("AddMap")]
        [Summary("AddMap <MapName>")]
        [Remarks("Add A Map")]
        [CheckAdmin]
        public async Task AddMap(string mapName)
        {
            var server = ServerList.Load(Context.Guild);
            if (!server.Maps.Contains(mapName))
            {
                server.Maps.Add(mapName);
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
        [CheckAdmin]
        public async Task DeleteMap(string mapName)
        {
            var server = ServerList.Load(Context.Guild);
            if (server.Maps.Contains(mapName))
            {
                server.Maps.Remove(mapName);
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