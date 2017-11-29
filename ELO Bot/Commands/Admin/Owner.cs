using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace ELO_Bot.Commands.Admin
{
    [RequireOwner]
    public class Owner : ModuleBase
    {
        /// <summary>
        ///     Adds a list of keys to the premium list.
        ///     If there are duplicate keys, automatically remove them
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        [Command("addpremium")]
        [Summary("addpremium")]
        [Remarks("Bot Creator Command")]
        public async Task Addpremium(params string[] keys)
        {
            var i = 0;
            var duplicates = "Dupes:\n";
            if (CommandHandler.Keys == null)
            {
                CommandHandler.Keys = keys.ToList();
                await ReplyAsync("list replaced.");
                var obj1 = JsonConvert.SerializeObject(CommandHandler.Keys, Formatting.Indented);
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "setup/keys.json"), obj1);
                return;
            }
            foreach (var key in keys)
            {
                var dupe = false;
                foreach (var k in CommandHandler.Keys)
                    if (k == key)
                        dupe = true;
                if (!dupe)
                {
                    i++;
                    CommandHandler.Keys.Add(key); //NO DUPES
                }
                else
                {
                    duplicates += $"{key}\n";
                }
            }
            await ReplyAsync($"{keys.Length} Supplied\n" +
                             $"{i} Added\n" +
                             $"{duplicates}");
            var keyobject = JsonConvert.SerializeObject(CommandHandler.Keys, Formatting.Indented);
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "setup/keys.json"), keyobject);
        }

        /// <summary>
        ///     rename the bot using a the provided input
        /// </summary>
        /// <param name="name">the provided name for the bot</param>
        /// <returns></returns>
        [Command("botrename")]
        [Summary("botrename")]
        [Remarks("Bot Creator Command")]
        public async Task Help([Remainder] string name)
        {
            if (name.Length > 32)
                throw new Exception("Name length must be less than 32 characters");
            await Context.Client.CurrentUser.ModifyAsync(x => { x.Username = name; });
        }

        /// <summary>
        ///     backs up the current serverlist object to a permanent file in the backups folder.
        ///     also updates the serverlist file
        /// </summary>
        /// <returns></returns>
        [Command("backup")]
        [Summary("backup")]
        [Remarks("Backup the current state of the database")]
        public async Task Backup()
        {
            var contents = JsonConvert.SerializeObject(Servers.ServerList);
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "setup/serverlist.json"), contents);

            var time = $"{DateTime.UtcNow:dd - MM - yy HH.mm.ss}.txt";

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, $"setup/backups/{time}"), contents);

            await ReplyAsync($"Backup has been saved to serverlist.json and {time}");
        }

        [Command("VerifyPatreon")]
        [Summary("VerifyPatreon <@user>")]
        [Remarks("Varify a supporter of the top ELO Bot Patreon Tier")]
        public async Task VerifyPatreon(IUser user)
        {
            if (CommandHandler.VerifiedUsers == null)
            {
                CommandHandler.VerifiedUsers = new List<ulong>{user.Id};
            }
            else
            {
                CommandHandler.VerifiedUsers.Add(user.Id);
            }
            
            await ReplyAsync("User has been verified.");
            
            var verifiedusers = JsonConvert.SerializeObject(CommandHandler.VerifiedUsers);
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "setup/verified.json"), verifiedusers);
            await ReplyAsync("Object Saved");

            foreach (var server in ((DiscordSocketClient)Context.Client).Guilds)
            {
                try
                {
                    var patreon = server.GetUser(user.Id);
                    if (patreon != null)
                    {
                        var serverobject = Servers.ServerList.First(x => x.ServerId == server.Id);
                        var userprofile = serverobject.UserList.First(x => x.UserId == user.Id);
                        await patreon.ModifyAsync(x =>
                        {
                            x.Nickname = $"👑{userprofile.Points} ~ {userprofile.Username}";
                        });
                    }
                }
                catch
                {
                    //
                }
            }
        }
    }
}