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
    ///     Checks the commands against the blacklist to ensure that a blacklisted command is not run
    /// </summary>
    [CheckBlacklist]
    [CheckModerator]
    public class Game : InteractiveBase
    {
        /// <summary>
        ///     adds a win and increaces the points for the specified users.
        /// </summary>
        /// <param name="userlist">a seperated list of users ie. @user1 @user2 @user3...</param>
        /// <returns></returns>
        [Command("Win")]
        [Summary("Win <users>")]
        [Remarks("Add a win + win points for the specified users")]
        public async Task Win(params IUser[] userlist)
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            var points = server.Winamount;
            if (!(server.Winamount > 0))
                throw new Exception("ERROR this server's win modifier has not been set up yet.");

            await WinLossPoints(server, userlist.ToList(), true, points);
        }

        /// <summary>
        ///     add a loss to the specified players and decrease their points accordingly
        /// </summary>
        /// <param name="userlist">a seperated list of users ie. @user1 @user2 @user3...</param>
        /// <returns></returns>
        [Command("Lose")]
        [Summary("Lose <users>")]
        [Remarks("Add a loss to the specified users")]
        public async Task Lose(params IUser[] userlist)
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            var points = server.Lossamount;

            if (!(server.Lossamount > 0))
                throw new Exception("ERROR this server's loss modifier has not been set up yet.");

            await WinLossPoints(server, userlist.ToList(), false, points);
        }

        /// <summary>
        ///     exact inverse of the game command
        ///     runs checks for the parameters, lobbyname & game number
        ///     creates lists of users in both teams and modifies scores accordingly
        /// </summary>
        /// <param name="lobbyname">name of the lobby the game was originally hosted in</param>
        /// <param name="gamenumber">game number for the specified lobby</param>
        /// <param name="team">winning team ie. Team2 or Team2</param>
        /// <returns></returns>
        [Command("UndoGame")]
        [Summary("UndoGame <lobbyname> <gamenumber> <winningteam>")]
        [Remarks("Undo the results of a previous game")]
        public async Task UnWin(string lobbyname, int gamenumber, string team)
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            IMessageChannel channel = null;
            foreach (var chan in Context.Guild.Channels)
                if (string.Equals(chan.Name, lobbyname, StringComparison.CurrentCultureIgnoreCase))
                    channel = chan as IMessageChannel;

            if (channel == null)
            {
                var queuechannels = "";
                foreach (var chan in server.Queue)
                {
                    var getqueuechannels = await ((IGuild) Context.Guild).GetChannelAsync(chan.ChannelId);
                    queuechannels += $"{getqueuechannels.Name}\n";
                }

                await ReplyAsync("**ERROR:** Please specify the channel in which queue was created\n" +
                                 "Here are a list:\n" +
                                 $"{queuechannels}");
                return;
            }

            var game = server.Gamelist.FirstOrDefault(x => x.LobbyId == channel.Id
                                                           && x.GameNumber == gamenumber);

            if (game == null)
            {
                await ReplyAsync("ERROR: Invalid Game number/channel");
                return;
            }

            var team1 = new List<IUser>();
            var team2 = new List<IUser>();
            foreach (var user in game.Team1)
                try
                {
                    team1.Add(await ((IGuild) Context.Guild).GetUserAsync(user));
                }
                catch
                {
                    await ReplyAsync(
                        $"{server.UserList.FirstOrDefault(x => x.UserId == user)?.Username} was unavailable");
                }


            foreach (var user in game.Team2)
                try
                {
                    team2.Add(await ((IGuild) Context.Guild).GetUserAsync(user));
                }
                catch
                {
                    await ReplyAsync(
                        $"{server.UserList.FirstOrDefault(x => x.UserId == user)?.Username} was unavailable");
                }


            switch (team.ToLower())
            {
                case "team1":
                case "team 1":
                case "1":
                    await UndoWinLossPoints(server, team1, true, server.Winamount,
                        $"{lobbyname} {game.GameNumber} {team}");
                    await UndoWinLossPoints(server, team2, false, server.Lossamount,
                        $"{lobbyname} {game.GameNumber} {team}");
                    game.Result = null;
                    break;
                case "team2":
                case "team 2":
                case "2":
                    await UndoWinLossPoints(server, team2, true, server.Winamount,
                        $"{lobbyname} {game.GameNumber} {team}");
                    await UndoWinLossPoints(server, team1, false, server.Lossamount,
                        $"{lobbyname} {game.GameNumber} {team}");
                    game.Result = null;
                    break;
                default:
                    await ReplyAsync(
                        "Please specify a team in the following format `=game <lobby> <number> team1` or `=game <lobby> <number> team2`");
                    break;
            }
        }


        /// <summary>
        ///     runs checks for the parameters, lobbyname & game number
        ///     creates lists of users in both teams and modifies scores accordingly
        /// </summary>
        /// <param name="lobbyname">name of the lobby the game was originally hosted in</param>
        /// <param name="gamenumber">game number for the specified lobby</param>
        /// <param name="team">winning team ie. Team2 or Team2</param>
        /// <returns></returns>
        [Command("Game", RunMode = RunMode.Async)]
        [Summary("Game <lobbyname> <gamenumber> <winningteam>")]
        [Remarks("Automatically update wins/losses for the selected team")]
        public async Task Win(string lobbyname, int gamenumber, string team)
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            IMessageChannel channel = null;
            foreach (var chan in Context.Guild.Channels)
                if (string.Equals(chan.Name, lobbyname, StringComparison.CurrentCultureIgnoreCase))
                    channel = chan as IMessageChannel;

            if (channel == null)
            {
                var queuechannels = "";
                foreach (var chan in server.Queue)
                    try
                    {
                        var getqueuechannels = await ((IGuild) Context.Guild).GetChannelAsync(chan.ChannelId);
                        queuechannels += $"{getqueuechannels.Name}\n";
                    }
                    catch
                    {
                        //
                    }
                await ReplyAsync("**ERROR:** Please specify the channel in which queue was created\n" +
                                 "Here are a list:\n" +
                                 $"{queuechannels}");
                return;
            }

            var game = server.Gamelist.FirstOrDefault(x => x.LobbyId == channel.Id
                                                           && x.GameNumber == gamenumber);

            if (game == null)
            {
                await ReplyAsync("ERROR: Invalid Game number/channel");
                return;
            }

            if (game.Result != null)
            {
                if (game.Result is true)
                    await ReplyAsync("Team1 is already recorded as winning this game.");

                if (game.Result is false)
                    await ReplyAsync("Team2 is already recorded as winning this game.");

                await ReplyAsync("Reply with `YES` to continue scoring this game, reply with `NO` to cancel");
                var response = await NextMessageAsync(timeout: TimeSpan.FromSeconds(30));
                if (response.Content.ToLower().Contains("yes"))
                {
                    //
                }
                else
                {
                    await ReplyAsync("Command Aborted.");
                    return;
                }
            }

            var team1 = new List<IUser>();
            var team2 = new List<IUser>();
            foreach (var user in game.Team1)
                try
                {
                    team1.Add(await ((IGuild) Context.Guild).GetUserAsync(user));
                }
                catch
                {
                    await ReplyAsync(
                        $"{server.UserList.FirstOrDefault(x => x.UserId == user)?.Username} was unavailable");
                }


            foreach (var user in game.Team2)
                try
                {
                    team2.Add(await ((IGuild) Context.Guild).GetUserAsync(user));
                }
                catch
                {
                    await ReplyAsync(
                        $"{server.UserList.FirstOrDefault(x => x.UserId == user)?.Username} was unavailable");
                }

            switch (team.ToLower())
            {
                case "team1":
                    await WinLossPoints(server, team1, true, server.Winamount, $"{lobbyname} {game.GameNumber} {team}");
                    await WinLossPoints(server, team2, false, server.Lossamount,
                        $"{lobbyname} {game.GameNumber} {team}");
                    game.Result = true;
                    break;
                case "team2":
                    await WinLossPoints(server, team2, true, server.Winamount, $"{lobbyname} {game.GameNumber} {team}");
                    await WinLossPoints(server, team1, false, server.Lossamount,
                        $"{lobbyname} {game.GameNumber} {team}");
                    game.Result = false;
                    break;
                default:
                    await ReplyAsync(
                        "Please specify a team in the following format `=game <lobby> <number> team1` or `=game <lobby> <number> team2`");
                    break;
            }
        }

        /// <summary>
        ///     For given server and users
        ///     if win = true
        ///     subtract one win and given points
        ///     if win = false
        ///     subtract one loss and add given points
        /// </summary>
        /// <param name="server"> current server's object</param>
        /// <param name="users"> users to be modified.</param>
        /// <param name="win"> check if the provided users won or lost</param>
        /// <param name="points">default points for the user's to be modified.</param>
        /// <param name="gametext">String of the game info</param>
        /// <returns></returns>
        public async Task UndoWinLossPoints(Servers.Server server, List<IUser> users, bool win, int points,
            string gametext = null)
        {
            var embed = new EmbedBuilder();
            foreach (var user in users)
            {
                var usr = server.UserList.FirstOrDefault(x => x.UserId == user.Id);

                if (usr == null || user.Id != usr.UserId) continue;
                {
                    //checks against possible ranks for each user. 
                    //if the user has a rank that has a different point modifier to the server's one, then modify 
                    //their points according to their rank
                    //if there is no role then ignore this.
                    try
                    {
                        var toprole = server.Ranks.Where(x => x.Points <= usr.Points).Max(x => x.Points);

                        var top = server.Ranks.Where(x => x.Points == toprole);

                        try
                        {
                            var rank = top.First();
                            if (rank.WinModifier != 0 && win)
                                points = rank.WinModifier;
                            else if (rank.LossModifier != 0 && !win)
                                points = rank.LossModifier;
                        }
                        catch
                        {
                            //
                        }
                    }
                    catch
                    {
                        //
                    }


                    if (win)
                    {
                        usr.Points = usr.Points - points;
                        usr.Wins--;
                        embed.AddField($"{usr.Username} [-{points}]", $"Points: **{usr.Points}**\n" +
                                                                      $"W/L: **[{usr.Wins}/{usr.Losses}]**");
                        embed.Color = Color.Blue;
                    }
                    else
                    {
                        points = Math.Abs(points);
                        usr.Points = usr.Points + points;
                        usr.Losses--;
                        if (usr.Points < 0)
                            usr.Points = 0;
                        embed.AddField($"{usr.Username} [+{points}]", $"Points: **{usr.Points}**\n" +
                                                                      $"W/L: **[{usr.Wins}/{usr.Losses}]**");
                        embed.Color = Color.Blue;
                    }
                    try
                    {
                        await ((IGuildUser) user).ModifyAsync(x => { x.Nickname = $"{usr.Points} ~ {usr.Username}"; });
                    }
                    catch
                    {
                        //
                    }

                    await CheckRank(server, user, usr);
                }
            }
            embed.Title = gametext != null ? $"Game Undone ({gametext})" : "Game Undone";

            await ReplyAsync("", false, embed.Build());
        }

        /// <summary>
        ///     For given server and users
        ///     if win = true
        ///     add one win and add given points
        ///     if win = false
        ///     add one loss and subtract given points
        /// </summary>
        /// <param name="server"> current server's object</param>
        /// <param name="users"> users to be modified.</param>
        /// <param name="win"> check if the provided users won or lost</param>
        /// <param name="points">default points for the user's to be modified.</param>
        /// <param name="gametext">Game text information</param>
        /// <returns></returns>
        public async Task WinLossPoints(Servers.Server server, List<IUser> users, bool win, int points,
            string gametext = null)
        {
            var embed = new EmbedBuilder();
            foreach (var user in users)
            {
                var usr = server.UserList.FirstOrDefault(x => x.UserId == user.Id);


                if (usr == null || user.Id != usr.UserId) continue;
                {
                    //checks against possible ranks for each user. 
                    //if the user has a rank that has a different point modifier to the server's one, then modify 
                    //their points according to their rank
                    //if there is no role then ignore this.
                    try
                    {
                        var toprole = server.Ranks.Where(x => x.Points <= usr.Points).Max(x => x.Points);
                        var top = server.Ranks.Where(x => x.Points == toprole);

                        try
                        {
                            var rank = top.First();
                            if (rank.WinModifier != 0 && win)
                                points = rank.WinModifier;
                            else if (rank.LossModifier != 0 && !win)
                                points = rank.LossModifier;
                        }
                        catch
                        {
                            //
                        }
                    }
                    catch
                    {
                        //
                    }


                    if (win)
                    {
                        usr.Points = usr.Points + points;
                        usr.Wins++;
                        embed.AddField($"{usr.Username} WON (+{points})", $"Points: **{usr.Points}**\n" +
                                                                          $"W/L: **[{usr.Wins}/{usr.Losses}]**");
                        embed.Color = Color.Green;
                    }
                    else
                    {
                        points = Math.Abs(points);
                        usr.Points = usr.Points - points;
                        usr.Losses++;
                        if (usr.Points < 0)
                            usr.Points = 0;
                        embed.AddField($"{usr.Username} LOST (-{points})", $"Points: **{usr.Points}**\n" +
                                                                           $"W/L: **[{usr.Wins}/{usr.Losses}]**");
                        embed.Color = Color.Red;
                    }
                    try
                    {
                        await ((IGuildUser) user).ModifyAsync(x => { x.Nickname = $"{usr.Points} ~ {usr.Username}"; });
                    }
                    catch
                    {
                        //
                    }
                    await CheckRank(server, user, usr);
                }
            }

            if (gametext != null)
                embed.Title = $"Game Decided: {gametext}";

            await ReplyAsync("", false, embed.Build());
        }

        public async Task CheckRank(Servers.Server server, IUser user, Servers.Server.User subject)
        {
            if (server.Ranks.Count == 0)
                return;

            foreach (var role in server.Ranks)
            {
                var u = user as IGuildUser;
                var r = Context.Guild.GetRole(role.RoleId);
                if (u != null && u.RoleIds.Contains(role.RoleId))
                    await u.RemoveRoleAsync(r);
            }
            try
            {
                var toprole = server.Ranks.Where(x => x.Points <= subject.Points).Max(x => x.Points);
                var top = server.Ranks.Where(x => x.Points == toprole);

                try
                {
                    var newrole = Context.Guild.GetRole(top.First().RoleId);
                    await ((IGuildUser) user).AddRoleAsync(newrole);
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