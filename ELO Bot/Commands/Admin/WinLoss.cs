using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using ELO_Bot.Preconditions;

namespace ELO_Bot.Commands.Admin
{
    /// <summary>
    ///     denies users that aren't administrators from accesing these commands.
    /// </summary>
    [CheckBlacklist]
    [CheckAdmin]
    public class WinLoss : ModuleBase
    {
        /// <summary>
        ///     remove a single win from the provided users
        /// </summary>
        /// <param name="userlist"></param>
        /// <returns></returns>
        [Command("DelWin")]
        [Summary("DelWin <users>")]
        [Remarks("remove one win from the specified users")]
        public async Task DelWin(params IUser[] userlist)
        {
            var embed = new EmbedBuilder();

            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            foreach (var user in userlist)
            {
                var success = false;
                var userval = 0;
                foreach (var subject in server.UserList)
                    if (subject.UserId == user.Id)
                    {
                        subject.Wins = subject.Wins - 1;
                        if (subject.Wins < 0)
                            subject.Wins = 0;
                        success = true;
                        userval = subject.Wins;
                    }
                if (!success)
                    embed.AddField($"{user.Username} ERROR", "Not Registered");
                else
                    embed.AddField($"{user.Username} MODIFIED", "Removed: -1\n" +
                                                                $"Current wins: {userval}");
            }
            embed.Color = Color.Green;
            await ReplyAsync("", false, embed.Build());
        }

        /// <summary>
        ///     remove a single loss from the provided users
        /// </summary>
        /// <param name="userlist"></param>
        /// <returns></returns>
        [Command("DelLose")]
        [Summary("DelLose <users>")]
        [Remarks("remove a single loss from the specified users")]
        public async Task DelLose(params IUser[] userlist)
        {
            var embed = new EmbedBuilder();

            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            foreach (var user in userlist)
            {
                var success = false;
                var userval = 0;
                foreach (var subject in server.UserList)
                    if (subject.UserId == user.Id)
                    {
                        subject.Losses = subject.Losses - 1;
                        if (subject.Losses < 0)
                            subject.Losses = 0;
                        success = true;
                        userval = subject.Losses;
                    }
                if (!success)
                    embed.AddField($"{user.Username} ERROR", "Not Registered");
                else
                    embed.AddField($"{user.Username} MODIFIED", "Removed: -1\n" +
                                                                $"Current Losses: {userval}");
            }
            embed.Color = Color.Green;
            await ReplyAsync("", false, embed.Build());
        }
    }
}