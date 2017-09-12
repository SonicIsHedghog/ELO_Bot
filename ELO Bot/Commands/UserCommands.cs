using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ELO_Bot.Commands
{
    [RequireTopic]
    [RequireContext(ContextType.Guild)]
    public class UserCommands : ModuleBase
    {
        [Command("Register")]
        [Summary("Register <username>")]
        [Remarks("registers the user in the server")]
        public async Task Register(string username = null)
        {
            var embed = new EmbedBuilder();

            if (username == null)
            {
                embed.AddField("ERROR", "Please specify a name to be registered with");
                embed.WithColor(Color.Red);
                await ReplyAsync("", false, embed.Build());
                return;
            }


            var user = new ServerList.Server.User
            {
                UserId = Context.User.Id,
                Username = username,
                Points = 0
            };

            var server = ServerList.Load(Context.Guild);
            if (server.UserList.Count >= 20 && !server.IsPremium)
                {
                    embed.AddField("ERROR",
                        "Free User limit has been hit. To upgrade the limit from 20 users to unlimited users, Purchase premium here: https://rocketr.net/buy/0e79a25902f5");
                    await ReplyAsync("", false, embed.Build());
                    return;
                }

            if (server.UserList.Any(member => member.UserId == Context.User.Id))
            {
                var userprofile = server.UserList.FirstOrDefault(x => x.UserId == Context.User.Id);
                embed.AddField("ERROR",
                    "You are already registered, if you wish to be renamed talk to an administrator");

                if (!(Context.User as IGuildUser).RoleIds.Contains(server.RegisterRole) && server.RegisterRole != 0)
                {
                        try
                        {
                            var serverrole = Context.Guild.GetRole(server.RegisterRole);
                            try
                            {
                                await (Context.User as IGuildUser).AddRoleAsync(serverrole);
                            }
                            catch
                            {
                                embed.AddField("ERROR", "User Role Unable to be modified");
                            }
                        }
                        catch
                        {
                            embed.AddField("ERROR", "Register Role is Unavailable");
                        }


                }

                try
                {
                    await (Context.User as IGuildUser).ModifyAsync(x => { x.Nickname = $"{userprofile.Points} ~ {username}"; });
                }
                catch
                {
                    embed.AddField("ERROR", "Username Unable to be modified (Permissions are above the bot)");
                }

                embed.WithColor(Color.Red);

                await ReplyAsync("", false, embed.Build());
                return;
            }

            server.UserList.Add(user);
            ServerList.Saveserver(server);
            embed.AddField($"{Context.User.Username} registered as {username}", $"{server.Registermessage}");
            embed.WithColor(Color.Blue);
            try
            {
                await (Context.User as IGuildUser).ModifyAsync(x => { x.Nickname = $"0 ~ {username}"; });
            }
            catch
            {
                embed.AddField("ERROR", "Username Unable to be modified (Permissions are above the bot)");
            }
            if (server.RegisterRole != 0)
                try
                {
                    var serverrole = Context.Guild.GetRole(server.RegisterRole);
                    try
                    {
                        await (Context.User as IGuildUser).AddRoleAsync(serverrole);
                    }
                    catch
                    {
                        embed.AddField("ERROR", "User Role Unable to be modified");
                    }
                }
                catch
                {
                    embed.AddField("ERROR", "Register Role is Unavailable");
                }
            await ReplyAsync("", false, embed.Build());
        }

        [Command("GetUser")]
        [Summary("GetUser <@user>")]
        [Remarks("checks stats about a user")]
        [CheckRegistered]
        public async Task GetUser(IUser user)
        {
            var embed = new EmbedBuilder();
            var server = ServerList.Load(Context.Guild);
            var userlist = server.UserList;
            var orderlist = server.UserList.OrderBy(x => x.Points).Reverse().ToList();
            foreach (var usr in userlist)
                if (usr.UserId == user.Id)
                {
                    embed.AddField($"{usr.Username}", $"Points: {usr.Points}\n" +
                                                      $"Wins: {usr.Wins}\n" +
                                                      $"Losses: {usr.Losses}\n" +
                                                      $"Leaderboard Rank: {orderlist.FindIndex(x => x.UserId == user.Id) + 1}");
                    await ReplyAsync("", false, embed.Build());
                    return;
                }

            await ReplyAsync("User Unavailable");
        }

        [Command("Leaderboard")]
        [Summary("Leaderboard")]
        [Remarks("Displays Rank Leaderboard (Top 20 if too large)")]
        [CheckRegistered]
        public async Task LeaderBoard()
        {
            var embed = new EmbedBuilder();
            var server = ServerList.Load(Context.Guild);
            var desc = "";

            var orderlist = server.UserList.OrderBy(x => x.Points).Reverse().ToList();

            var i = 0;
            foreach (var user in orderlist)
            {
                i++;
                if (i <= 20)
                    desc += $"{i}. {user.Username} - {user.Points}\n";
            }
            embed.WithFooter(x =>
            {
                x.Text = $"Usercount = {i}";
                x.IconUrl = Context.Client.CurrentUser.GetAvatarUrl();
            });
            embed.AddField("LeaderBoard", desc);
            embed.Color = Color.Blue;
            await ReplyAsync("", false, embed.Build());
        }

        [Command("ranks")]
        [Summary("ranks")]
        [Remarks("display all Ranked Roles")]
        [CheckRegistered]
        public async Task List()
        {
            var embed = new EmbedBuilder();
            var server = ServerList.Load(Context.Guild);
            var orderedlist = server.Ranks.OrderBy(x => x.Points).Reverse();
            var desc = "Points - Role\n";
            foreach (var lev in orderedlist)
            {
                string rolename;
                try
                {
                    rolename = Context.Guild.GetRole(lev.RoleId).Name;
                }
                catch
                {
                    rolename = $"ERR: {lev.RoleId}";
                }

                desc += $"`{lev.Points}` - {rolename}\n";
            }
            embed.AddField("Ranks", desc);
            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }
    }
}