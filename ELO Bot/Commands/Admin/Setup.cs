using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using ELO_Bot.Preconditions;
using Newtonsoft.Json;

namespace ELO_Bot.Commands.Admin
{
    /// <summary>
    ///     ensure only admins can use the commands
    /// </summary>
    [RequireContext(ContextType.Guild)]
    [CheckBlacklist]
    [CheckAdmin]
    public class Setup : ModuleBase
    {
        private readonly CommandService _service;

        public Setup(CommandService service)
        {
            _service = service;
        }

        /// <summary>
        ///     Command to inisialise the server configuration (if it wasn't done initially)
        /// </summary>
        /// <returns></returns>
        [Command("Initialise")]
        [Summary("Inisialise")]
        [Remarks("Run this command to add your server to the serverlist")]
        public async Task Initialise()
        {
            if (Servers.ServerList.All(x => x.ServerId != Context.Guild.Id))
            {
                var server = new Servers.Server
                {
                    ServerId = Context.Guild.Id,
                    UserList = new List<Servers.Server.User>()
                };

                Servers.ServerList.Add(server);
                await ReplyAsync("Server Initialised, users may now register");
                return;
            }

            await ReplyAsync("Server has already been initialised. Denied.");
        }

        /// <summary>
        ///     set the current channel for game announcements
        /// </summary>
        /// <returns></returns>
        [Command("SetAnnouncements")]
        [Summary("SetAnnouncements")]
        [Remarks("Set the current channel for game announcements")]
        public async Task SetAnnounce()
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            server.AnnouncementsChannel = Context.Channel.Id;
            await ReplyAsync("GameAnnouncements will now be posted in this channel");
        }

        /// <summary>
        ///     set the role that users are given upon registering.
        /// </summary>
        /// <param name="role">role to give users. ie. @role</param>
        /// <returns></returns>
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

            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            server.RegisterRole = role.Id;
            embed.AddField("Complete!", $"Upon registering, users will now be added to the role: {role.Name}");
            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }

        /// <summary>
        ///     toggle whether users are removed from queues when going idle/offline
        /// </summary>
        /// <returns></returns>
        [Command("ToggleAutoRemove")]
        [Summary("ToggleAutoRemove")]
        [Remarks("Set if users are removed from the queue when going offline")]
        public async Task IdleRemove()
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            server.Autoremove = !server.Autoremove;
            if (server.Autoremove)
                await ReplyAsync("Users will be removed from queues if they go offline");
            else
                await ReplyAsync("Users will not be removed from queues if they go offline");
        }

        /// <summary>
        ///     toggle whether to block users from joining more than one queue at a time.
        /// </summary>
        /// <returns></returns>
        [Command("ToggleMultiQueue")]
        [Summary("ToggleMultiQueue")]
        [Remarks("Toggle wether users are able to join more than one queue at a time")]
        public async Task MultiQueue()
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            server.BlockMultiQueueing = !server.BlockMultiQueueing;
            if (server.BlockMultiQueueing)
                await ReplyAsync("Users will only be allowed in one queue at any one time.");
            else
                await ReplyAsync("Users are now allowed in any number of queues they want.");
        }

        /// <summary>
        ///     command for upgrading a server to the premium version of ELO Bot. Ie, have 20+ users registered
        /// </summary>
        /// <param name="key">key to input ie. 1234-5678-1234-5678</param>
        /// <returns></returns>
        [Command("Premium")]
        [Summary("Premium <key>")]
        [Remarks("Upgrade the server to premium and increase the userlimit to unlimited")]
        public async Task PremiumCommand(string key = null)
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            var embed = new EmbedBuilder();

            if (key == null)
            {
                embed.AddField("Premium",
                    "Premium allows servers to use the bot with more than 20 users to purchase it, check here: https://rocketr.net/buy/0e79a25902f5");
                embed.Color = Color.Blue;
                await ReplyAsync("", false, embed.Build());
                return;
            }


            if (CommandHandler.Keys.Contains(key))
            {
                if (server.IsPremium)
                {
                    embed.AddField("ERROR",
                        "This server is already premium, to avoid wasting your key, you may use it on any other server that isnt premium");
                    embed.Color = Color.Red;
                    await ReplyAsync("", false, embed.Build());
                    return;
                }
                CommandHandler.Keys.Remove(key);
                var obj = JsonConvert.SerializeObject(CommandHandler.Keys, Formatting.Indented);
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "setup/keys.json"), obj);

                server.IsPremium = true;
                server.PremiumKey = key;
                embed.AddField("SUCCESS",
                    "This server has been upgraded to premium, userlimits for registrations is now greater than 20!");
                embed.Color = Color.Green;
                await ReplyAsync("", false, embed.Build());
            }
            else
            {
                embed.AddField("ERROR INVALID KEY",
                    "Premium allows servers to use the bot with more than 20 users to purchase it, check here: https://rocketr.net/buy/0e79a25902f5");
                embed.Color = Color.Red;
                await ReplyAsync("", false, embed.Build());
            }
        }

        /// <summary>
        ///     set the rolethat is given bot administrator permissions
        /// </summary>
        /// <param name="adminrole"></param>
        /// <returns></returns>
        [Command("SetAdmin")]
        [Summary("SetAdmin <@role>")]
        [Remarks("sets the configurable admin role")]
        public async Task SetAdmin(IRole adminrole)
        {
            var embed = new EmbedBuilder();

            var s1 = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);

            s1.AdminRole = adminrole.Id;
            embed.AddField("Complete!", $"People with the role {adminrole.Mention} can now use admin commands");
            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }

        /// <summary>
        ///     set the role that is given moderator permissions. ie. Are able to use the `game` commands
        /// </summary>
        /// <param name="modRole"></param>
        /// <returns></returns>
        [Command("SetMod")]
        [Summary("SetMod <@role>")]
        [Remarks("Sets the moderator role (point updating access)")]
        public async Task SetMod(IRole modRole)
        {
            var embed = new EmbedBuilder();

            var s1 = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);

            s1.ModRole = modRole.Id;
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

        /// <summary>
        ///     set the welcome message given to users when they register
        /// </summary>
        /// <param name="message">message to be displayed</param>
        /// <returns></returns>
        [Command("SetRegisterMessage")]
        [Summary("SetRegistermessage <message>")]
        [Remarks("sets the configurable registration message")]
        public async Task SetWelcome([Remainder] string message = null)
        {
            var embed = new EmbedBuilder();

            var s1 = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);

            if (message == null)
            {
                embed.AddField("ERROR", "Please specify a welcome message for users");
                embed.WithColor(Color.Red);
                await ReplyAsync("", false, embed.Build());
                return;
            }

            s1.Registermessage = message;
            embed.AddField("Complete!", "Registration Message will now include the following:\n" +
                                        $"{message}");
            embed.WithColor(Color.Blue);
            await ReplyAsync("", false, embed.Build());
        }

        /// <summary>
        ///     sets the points removed from a user when they lose.
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        [Command("ModifyLoss")]
        [Summary("ModifyLoss <points>")]
        [Remarks("Sets the servers Loss amount")]
        public async Task Lose(int points)
        {
            var embed = new EmbedBuilder();
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
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
            embed.AddField("Success", $"Upon losing, users will now lose {points} points");
            embed.Color = Color.Green;
            await ReplyAsync("", false, embed.Build());
        }

        /// <summary>
        ///     set the points given to a user when they win
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        [Command("ModifyWin")]
        [Summary("ModifyWin <points>")]
        [Remarks("Sets the servers Win amount")]
        public async Task Win(int points)
        {
            var embed = new EmbedBuilder();
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
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
            embed.AddField("Success", $"Upon Winning, users will now gain {points} points");
            embed.Color = Color.Green;
            await ReplyAsync("", false, embed.Build());
        }

        /// <summary>
        ///     server owner only command, resets all user scores on the scoreboard.
        /// </summary>
        /// <returns></returns>
        [Command("ScoreboardReset", RunMode = RunMode.Async)]
        [Summary("ScoreboardReset")]
        [Remarks("Reset Points, Wins and Losses for all users in the server")]
        [ServerOwner]
        public async Task Reset()
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            await ReplyAsync("Working...");

            var reset = server.UserList.ToList();
            foreach (var user in reset)
            {
                if (user.Points != 0 || user.Wins != 0 || user.Losses != 0)
                {
                    user.Points = 0;
                    user.Wins = 0;
                    user.Losses = 0;
                }
            }
            server.UserList = reset;

            await ReplyAsync("Leaderboard Reset Complete!\n" +
                             "NOTE: Names and ranks will be reset over the next few minutes.");

            foreach (var user in reset)
            {
                try
                {
                    var us = await Context.Guild.GetUserAsync(user.UserId);
                    if (us.Nickname.StartsWith("0 ~ ")) continue;
                    await us.ModifyAsync(x =>
                    {
                        x.Nickname = $"0 ~ {user.Username}";
                    });
                    if (CommandHandler.VerifiedUsers != null)
                        if (CommandHandler.VerifiedUsers.Contains(us.Id))
                            await us.ModifyAsync(x => { x.Nickname = $"👑0 ~ {user.Username}"; });
                    await us.RemoveRolesAsync(server.Ranks.Select(x => Context.Guild.GetRole(x.RoleId)));
                    await Task.Delay(1000);
                }
                catch
                {
                    //
                }
            }

            await ReplyAsync("Reset fully complete.");
        }

        /// <summary>
        ///     list all commands unavailable to regilar users
        ///     these commands have been 'blacklisted' and are not able to be used by regular users.
        /// </summary>
        /// <returns></returns>
        [Command("Blacklist")]
        [Remarks("List blacklisted commands")]
        [Summary("Blacklist")]
        public async Task Blacklist()
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);

            if (server.CmdBlacklist.Count == 0)
            {
                await ReplyAsync("There are no blacklisted commands in this server");
                return;
            }

            var embed = new EmbedBuilder();
            embed.AddField("Blacklist", $"{string.Join("\n", server.CmdBlacklist)}");

            await ReplyAsync("", false, embed);
        }

        /// <summary>
        ///     add a command to the blacklist.
        /// </summary>
        /// <param name="cmdname">command name.</param>
        /// <returns></returns>
        [Command("BlacklistAdd")]
        [Remarks("Blacklist a command from all regular users.")]
        [Summary("BlacklistAdd <command-name>")]
        public async Task BlacklistAdd(string cmdname)
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);

            if (_service.Modules.SelectMany(module => module.Commands).Any(command =>
                string.Equals(command.Name, cmdname, StringComparison.CurrentCultureIgnoreCase)))
            {
                if (server.CmdBlacklist.Contains(cmdname.ToLower()))
                {
                    await ReplyAsync("Command already blacklisted.");
                }
                else
                {
                    server.CmdBlacklist.Add(cmdname.ToLower());
                    await ReplyAsync(
                        "Command added to blacklist. NOTE: Server Moderators and Administrators can still use this command");
                }
                return;
            }

            await ReplyAsync("No Matching Command.");
        }

        /// <summary>
        ///     remove a command from the blacklist
        /// </summary>
        /// <param name="cmdname">command name</param>
        /// <returns></returns>
        [Command("BlacklistDel")]
        [Remarks("Remove a blacklisted command")]
        [Summary("BlacklistDel <command-name>")]
        public async Task BlacklistDel(string cmdname)
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);

            if (_service.Modules.SelectMany(module => module.Commands).Any(command =>
                string.Equals(command.Name, cmdname, StringComparison.CurrentCultureIgnoreCase)))
            {
                if (server.CmdBlacklist.Contains(cmdname.ToLower()))
                {
                    server.CmdBlacklist.Remove(cmdname.ToLower());
                    await ReplyAsync(
                        "Command removed from blacklist. All users will be able to use it now.");
                }
                else
                {
                    throw new Exception("Command not blacklisted.");
                }
                return;
            }

            await ReplyAsync("No Matching Command.");
        }

        /// <summary>
        ///     server information
        /// </summary>
        /// <returns></returns>
        [Command("Server")]
        [Remarks("Stats and info about the bot & current server")]
        [Summary("Server")]
        [CheckRegistered]
        public async Task Stats()
        {
            var embed = new EmbedBuilder();
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);

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