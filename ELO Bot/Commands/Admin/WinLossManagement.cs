using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace ELO_Bot.Commands.Admin
{
    public class WinLossManagement : ModuleBase
    {
        [Command("win")]
        [Summary("Win <users>")]
        [Remarks("Add a win + win points for the specified users")]
        [CheckAdmin]
        public async Task Win(params IUser[] userlist)
        {
            var embed = new EmbedBuilder();
            var server = ServerList.Load(Context.Guild);
            var points = server.Winamount;
            if (!(server.Winamount > 0))
            {
                embed.AddField("ERROR", "You have not setup the server's win modifier yet");
                await ReplyAsync("", false, embed.Build());
                return;
            }

            foreach (var user in userlist)
                await WinLossPoints(server, user, true, points);
        }

        [Command("Lose")]
        [Summary("Lose <users>")]
        [Remarks("Add a loss to the specified users")]
        [CheckAdmin]
        public async Task Lose(params IUser[] userlist)
        {
            var embed = new EmbedBuilder();
            var server = ServerList.Load(Context.Guild);
            var points = server.Winamount;

            if (!(server.Lossamount > 0))
            {
                embed.AddField("ERROR", "You have not setup the server's loss modifier yet");
                await ReplyAsync("", false, embed.Build());
                return;
            }

            foreach (var user in userlist)
                await WinLossPoints(server, user, false, points);
        }

        [Command("game")]
        [Summary("game <lobbyname> <gamenumber> <winningteam>")]
        [Remarks("Automatically update wins/losses for the selected team")]
        public async Task win(string lobbyname, int gamenumber, string team)
        {
            var server = ServerList.Load(Context.Guild);
            IMessageChannel channel = null;
            foreach (var chan in (Context.Guild as SocketGuild).Channels)
            {
                if (chan.Name.ToLower() == lobbyname.ToLower())
                {
                    channel = chan as IMessageChannel;
                }
            }

            if (channel == null)
            {
                var queuechannels = "";
                foreach (var chan in server.Queue)
                {
                    var getqueuechannels = await Context.Guild.GetChannelAsync(chan.ChannelId);
                    queuechannels += $"{getqueuechannels.Name}\n";
                }
                await ReplyAsync("**ERROR:** Please specify the channel in which queue was created\n" +
                                 "Here are a list:\n" +
                                 $"{queuechannels}");
                return;
            }

            var game = server.Gamelist.FirstOrDefault(x => x.LobbyId == channel.Id
                                                           || x.GameNumber == gamenumber);
            var team1 = new List<IUser>();
            var team2 = new List<IUser>();
            foreach (var user in game.Team1)
                team1.Add(await Context.Guild.GetUserAsync(user));

            foreach (var user in game.Team2)
                team2.Add(await Context.Guild.GetUserAsync(user));
            var embed = new EmbedBuilder();
            var win = "";
            if (team.ToLower() == "team1")
            {
                foreach (var member in team1)
                    await WinLossPoints(server, member, true, server.Winamount);
                foreach (var member in team2)
                    await WinLossPoints(server, member, false, server.Lossamount);
            }
            else if (team.ToLower() == "team2")
            {
                foreach (var member in team2)
                    await WinLossPoints(server, member, true, server.Winamount);
                foreach (var member in team1)
                    await WinLossPoints(server, member, false, server.Lossamount);
            }
            else
            {
                await ReplyAsync(
                    "Please specify a team in the following format `=game <number> team1` or `=game <number> team2`");
            }
        }

        public async Task WinLossPoints(ServerList.Server server, IUser user, bool win, int points)
        {
            var embed = new EmbedBuilder();
            foreach (var usr in server.UserList)
                if (user.Id == usr.UserId)
                {
                    if (win)
                    {
                        usr.Points = usr.Points + points;
                        usr.Wins++;
                        embed.AddField($"{usr.Username} WON", $"User Won\n" +
                                                              $"Points: {usr.Points}\n" +
                                                              $"Wins: {usr.Wins}\n" +
                                                              $"Losses: {usr.Losses}");
                        embed.Color = Color.Green;
                    }
                    else
                    {
                        points = Math.Abs(points);
                        usr.Points = usr.Points - points;
                        usr.Losses++;
                        if (usr.Points < 0)
                            usr.Points = 0;
                        embed.AddField($"{usr.Username} LOST", $"User Lost\n" +
                                                               $"Points: {usr.Points}\n" +
                                                               $"Wins: {usr.Wins}\n" +
                                                               $"Losses: {usr.Losses}");
                        embed.Color = Color.Red;
                    }
                    try
                    {
                        await (user as IGuildUser).ModifyAsync(x => { x.Nickname = $"{usr.Points} ~ {usr.Username}"; });
                    }
                    catch
                    {
                        //
                    }
                    await CheckRank(server, user, usr);
                    break;
                }
            ServerList.Saveserver(server);
            await ReplyAsync("", false, embed.Build());
        }

        public async Task CheckRank(ServerList.Server server, IUser user, ServerList.Server.User subject)
        {
            foreach (var role in server.Ranks)
            {
                var u = user as IGuildUser;
                var r = Context.Guild.GetRole(role.RoleId);
                if (u.RoleIds.Contains(role.RoleId))
                    await u.RemoveRoleAsync(r);
            }
            try
            {
                var toprole = server.Ranks.Where(x => x.Points <= subject.Points).Max(x => x.Points);
                var top = server.Ranks.Where(x => x.Points == toprole);
                try
                {
                    var newrole = Context.Guild.GetRole(top.FirstOrDefault().RoleId);
                    await (user as IGuildUser).AddRoleAsync(newrole);
                }
                catch
                {
                    //role has been deleted
                }
            }
            catch
            {
                // No available roles
            }
        }
    }
}