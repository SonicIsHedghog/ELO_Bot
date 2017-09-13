using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ELO_Bot.Commands.Admin
{
    [CheckAdmin]
    public class RankManagement : ModuleBase
    {
        [Command("addrank")]
        [Summary("addrank <points> <@role>")]
        [Remarks("set a Rank to join")]
        public async Task SetReg(int points, IRole role)
        {
            var embed = new EmbedBuilder();


            if (points == 0)
            {
                embed.AddField("ERROR", "Please specify a value greater than zero");
                await ReplyAsync("", false, embed.Build());
                return;
            }


            var server = ServerList.Load(Context.Guild);
            var add = new ServerList.Server.Ranking
            {
                Points = points,
                RoleId = role.Id
            };

            var containedlevel = server.Ranks.SingleOrDefault(x => x.Points == points);
            if (containedlevel != null)
            {
                embed.AddField("ERROR", "The Ranks list already contains a role with this number of points");
                await ReplyAsync("", false, embed.Build());
                return;
            }

            var containedrole = server.Ranks.SingleOrDefault(x => x.RoleId == role.Id);
            if (containedrole != null)
            {
                var oldpoints = containedrole.Points;
                server.Ranks.Remove(containedrole);
                server.Ranks.Add(add);
                embed.AddField("Rank Updated", $"Points: {oldpoints} => {points}\n" +
                                               $"{role.Name}");
            }
            else
            {
                server.Ranks.Add(add);
                embed.AddField("Added", $"Rank: {role.Name}\n" +
                                        $"Points: {points}");
            }

            ServerList.Saveserver(server);
            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }

        [Command("removerank")]
        [Summary("removerank <@role>")]
        [Remarks("remove a role from the Ranks list")]
        public async Task Remove(IRole role = null)
        {
            var embed = new EmbedBuilder();


            if (role == null)
            {
                embed.AddField("ERROR", "Please specify a role");
                await ReplyAsync("", false, embed.Build());
                return;
            }


            var server = ServerList.Load(Context.Guild);

            var containedrole = server.Ranks.SingleOrDefault(x => x.RoleId == role.Id);
            if (containedrole != null)
            {
                server.Ranks.Remove(containedrole);
                embed.AddField("SUCCESS", $"{role.Name} is no longer ranked");
            }
            else
            {
                embed.AddField("ERROR", "This role is not Ranked");
            }

            ServerList.Saveserver(server);
            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }
    }
}