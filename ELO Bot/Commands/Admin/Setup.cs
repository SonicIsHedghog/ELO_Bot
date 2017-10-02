using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using ELO_Bot.PreConditions;
using Newtonsoft.Json;

namespace ELO_Bot.Commands.Admin
{
    [RequireContext(ContextType.Guild)]
    [CheckAdmin]
    public class Setup : ModuleBase
    {
        [Command("SetAnnouncements")]
        [Summary("SetAnnouncements")]
        [Remarks("Set the current channel for game announcements")]
        public async Task SetAnnounce()
        {
            var server = ServerList.Load(Context.Guild);
            server.AnnouncementsChannel = Context.Channel.Id;
            ServerList.Saveserver(server);
            await ReplyAsync("GameAnnouncements will now be posted in this channel");
        }

        [Command("SetRegisterRole")]
        [Summary("SetRegisterRole <@role>")]
        [Remarks("Sets the role users will join when registering")]
        public async Task SetReg(IRole role = null)
        {
            var embed = new EmbedBuilder();

            if (role == null)
            {
                embed.AddField("ERROR", "Please specify a role for users to be added to upon registering");
                embed.WithColor(Color.Red);
                await ReplyAsync("", false, embed.Build());
                return;
            }

            var server = ServerList.Load(Context.Guild);
            server.RegisterRole = role.Id;
            ServerList.Saveserver(server);
            embed.AddField("Complete!", $"Upon registering, users will now be added to the role: {role.Name}");
            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }

        [Command("RemoveIdle")]
        [Summary("RemoveIdle")]
        [Remarks("Set if users are removed from the queue when going idle")]
        public async Task IdleRemove()
        {
            var server = ServerList.Load(Context.Guild);
            server.Autoremove = !server.Autoremove;
            ServerList.Saveserver(server);
            if (server.Autoremove)
                await ReplyAsync("Users will be removed from queues if they go idle or if they go offline");
            else
                await ReplyAsync("Users will be removed from queues only if they go offline");
        }

        [Command("Premium")]
        [Summary("Premium <key>")]
        [Remarks("Upgrade the server to premium and increase the userlimit to unlimited")]
        public async Task PremiumCommand(string key = null)
        {
            var server = ServerList.Load(Context.Guild);
            var embed = new EmbedBuilder();

            if (key == null)
            {
                embed.AddField("Premium",
                    "Premium allows servers to use the bot with more than 20 users to purchase it, check here: https://rocketr.net/buy/0e79a25902f5");
                embed.Color = Color.Blue;
                await ReplyAsync("", false, embed.Build());
                return;
            }


            if (Program.Keys.Contains(key))
            {
                if (server.IsPremium)
                {
                    embed.AddField("ERROR",
                        "This server is already premium, to avoid wasting your key, you may use it on any other server that isnt premium");
                    embed.Color = Color.Red;
                    await ReplyAsync("", false, embed.Build());
                    return;
                }
                Program.Keys.Remove(key);
                var obj = JsonConvert.SerializeObject(Program.Keys, Formatting.Indented);
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "setup/keys.json"), obj);

                server.IsPremium = true;
                server.PremiumKey = key;
                embed.AddField("SUCCESS",
                    "This server has been upgraded to premium, userlimits for registrations is now greater than 20!");
                embed.Color = Color.Green;
                await ReplyAsync("", false, embed.Build());
                ServerList.Saveserver(server);
            }
            else
            {
                embed.AddField("ERROR INVALID KEY",
                    "Premium allows servers to use the bot with more than 20 users to purchase it, check here: https://rocketr.net/buy/0e79a25902f5");
                embed.Color = Color.Red;
                await ReplyAsync("", false, embed.Build());
            }
        }

        [Command("SetAdmin")]
        [Summary("SetAdmin <@role>")]
        [Remarks("sets the configurable admin role")]
        public async Task SetAdmin(IRole adminrole)
        {
            var embed = new EmbedBuilder();

            var s1 = ServerList.Load(Context.Guild);

            s1.AdminRole = adminrole.Id;
            ServerList.Saveserver(s1);
            embed.AddField("Complete!", $"People with the role {adminrole.Mention} can now use admin commands");
            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }

        [Command("SetMod")]
        [Summary("SetMod <@role>")]
        [Remarks("Sets the moderator role (point updating access)")]
        public async Task SetMod(IRole modRole)
        {
            var embed = new EmbedBuilder();

            var s1 = ServerList.Load(Context.Guild);

            s1.ModRole = modRole.Id;
            ServerList.Saveserver(s1);
            embed.AddField("Complete!",
                $"People with the role {modRole.Mention} can now use the following commands:\n" +
                "```\n" +
                "=win <@user1> <@user2>...\n" +
                "=lose <@user1> <@user2>...\n" +
                "=game <lobby> <match-no.> <team1/team2>\n" +
                "```");
            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }

        [Command("SetWelcome")]
        [Summary("SetWelcome <message>")]
        [Remarks("sets the configurable welcome message")]
        public async Task SetWelcome([Remainder] string message = null)
        {
            var embed = new EmbedBuilder();

            var s1 = ServerList.Load(Context.Guild);

            if (message == null)
            {
                embed.AddField("ERROR", "Please specify a welcome message for users");
                embed.WithColor(Color.Red);
                await ReplyAsync("", false, embed.Build());
                return;
            }

            s1.Registermessage = message;
            ServerList.Saveserver(s1);
            embed.AddField("Complete!", "Registration Message will now include the following:\n" +
                                        $"{message}");
            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }

        [Command("ModifyLoss")]
        [Summary("ModifyLoss <points>")]
        [Remarks("Sets the servers Loss amount")]
        public async Task Lose(int points)
        {
            var embed = new EmbedBuilder();
            var server = ServerList.Load(Context.Guild);
            if (points == 0)
            {
                embed.AddField("ERROR", "Please supply a number that isnt 0");
                embed.Color = Color.Red;
                await ReplyAsync("", false, embed.Build());
                return;
            }
            if (points <= 0)
                points = Math.Abs(points);
            server.Lossamount = points;
            ServerList.Saveserver(server);
            embed.AddField("Success", $"Upon losing, users will now lose {points} points");
            embed.Color = Color.Green;
            await ReplyAsync("", false, embed.Build());
        }

        [Command("ModifyWin")]
        [Summary("ModifyWin <points>")]
        [Remarks("Sets the servers Win amount")]
        public async Task Win(int points)
        {
            var embed = new EmbedBuilder();
            var server = ServerList.Load(Context.Guild);
            if (points == 0)
            {
                embed.AddField("ERROR", "Please supply a number that isnt 0");
                embed.Color = Color.Red;
                await ReplyAsync("", false, embed.Build());
                return;
            }
            if (points <= 0)
                points = Math.Abs(points);
            server.Winamount = points;
            ServerList.Saveserver(server);
            embed.AddField("Success", $"Upon Winning, users will now gain {points} points");
            embed.Color = Color.Green;
            await ReplyAsync("", false, embed.Build());
        }

        [Command("ScoreboardReset", RunMode = RunMode.Async)]
        [Summary("ScoreboardReset")]
        [Remarks("Reset Points, Wins and Losses for all users in the server")]
        [ServerOwner]
        public async Task Reset()
        {
            var server = ServerList.Load(Context.Guild);
            await ReplyAsync("Working...\n" +
                             $"Estimated Reset time = {server.UserList.Count * 3} seconds");
            foreach (var user in server.UserList)
            {
                user.Points = 0;
                user.Wins = 0;
                user.Losses = 0;
                try
                {
                    var delay = 0;
                    var u = await Context.Guild.GetUserAsync(user.UserId);
                    if (!u.Nickname.StartsWith("0 ~ "))
                        try
                        {
                            await u.ModifyAsync(x => { x.Nickname = $"0 ~ {user.Username}"; });
                            delay = 1000;
                        }
                        catch
                        {
                            //
                        }
                    try
                    {
                        await u.RemoveRolesAsync(server.Ranks.Select(x => Context.Guild.GetRole(x.RoleId)));
                        delay = 1000;
                    }
                    catch
                    {
                        //
                    }
                    await Task.Delay(delay);
                }
                catch
                {
                    //
                }
            }


            ServerList.Saveserver(server);
            await ReplyAsync("Leaderboard Reset Complete!");
        }

        [Command("Server")]
        [Remarks("Stats and info about the bot & current server")]
        [Summary("Server")]
        [CheckRegistered]
        public async Task Stats()
        {
            var embed = new EmbedBuilder();
            var server = ServerList.Load(Context.Guild);

            try
            {
                var admin = Context.Guild.GetRole(server.AdminRole);
                embed.AddField("Admin Role", $"{admin.Name}");
            }
            catch
            {
                embed.AddField("Admin Role", "N/A");
            }

            try
            {
                var admin = Context.Guild.GetRole(server.ModRole);
                embed.AddField("Mod Role", $"{admin.Name}");
            }
            catch
            {
                embed.AddField("Mod Role", "N/A");
            }

            try
            {
                var ann = await Context.Guild.GetChannelAsync(server.AnnouncementsChannel);
                embed.AddField("Announcements Channel", $"{ann.Name}");
            }
            catch
            {
                embed.AddField("Announcements Channel", "N/A");
            }

            embed.AddField("Is Premium?", $"{server.IsPremium}");
            embed.AddField("Points Per Win/Loss", $"{server.Winamount}/{server.Lossamount}");

            try
            {
                embed.AddField("Counts", $"Lobbies: {server.Queue.Count}\n" +
                                         $"Ranks: {server.Ranks.Count}\n" +
                                         $"Registered Users: {server.UserList.Count}");
            }
            catch
            {
                embed.AddField("Counts", "Error");
            }

            embed.AddField("Registration Message", $"{server.Registermessage}");

            try
            {
                var ann = Context.Guild.GetRole(server.RegisterRole);
                embed.AddField("Registration Role", $"{ann.Name}");
            }
            catch
            {
                embed.AddField("Registration Role", "N/A");
            }

            await ReplyAsync("", false, embed.Build());
        }
    }
}