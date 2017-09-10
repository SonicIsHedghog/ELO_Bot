using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace ELO_Bot.Commands
{
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class Other : ModuleBase
    {
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

        private readonly CommandService _service;

        public Other(CommandService service)
        {
            _service = service;
        }

        [Command("AdminHelp")]
        [Summary("AdminHelp")]
        [Remarks("Admin Commands")]
        [CheckAdmin]
        [CheckRegistered]
        public async Task AdminHelp()
        {
            var embed = new EmbedBuilder();
            foreach (var module in _service.Modules)
                if (module.Name.ToLower() == "admin" || module.Name.ToLower() == "winloss" || module.Name.ToLower() == "ranking")
                {
                    var desc = "";
                    foreach (var command in module.Commands)
                        desc += $"{Config.Load().Prefix}{command.Summary} - {command.Remarks}\n";
                    embed.AddField(module.Name, desc);
                }

            embed.AddField("Developed By PassiveModding", "Support Server: https://discord.gg/n2Vs38n \n" +
                                                          "Patreon: https://www.patreon.com/passivebot  \n" +
                                                          "Invite the BOT: https://goo.gl/mbfnjj");
            await ReplyAsync("", false, embed.Build());
        }
        [Command("Help")]
        [Summary("Help")]
        [Remarks("Small amount of info about the bot and stuff")]
        public async Task Help()
        {
            var embed = new EmbedBuilder();
            foreach (var module in _service.Modules)
                if (module.Name.ToLower() != "owner" && module.Name.ToLower() != "admin" && module.Name.ToLower() != "winloss" && module.Name.ToLower() != "ranking")
                {
                    var desc = "";
                    foreach (var command in module.Commands)
                        desc += $"{Config.Load().Prefix}{command.Summary} - {command.Remarks}\n";
                    embed.AddField(module.Name, desc);
                }

            embed.AddField("Developed By PassiveModding", "Support Server: https://discord.gg/n2Vs38n \n" +
                                                          "Patreon: https://www.patreon.com/passivebot  \n" +
                                                          "Invite the BOT: https://goo.gl/mbfnjj");
            await ReplyAsync("", false, embed.Build());
        }


        [Command("Stats")]
        [Remarks("Stats and info about the bot & current server")]
        [Summary("Stats")]
        public async Task Stats()
        {
            var server = ServerList.Load(Context.Guild);
            var embed = new EmbedBuilder();
            embed.AddField("Status", server.IsPremium ? $"Premium : {server.PremiumKey}" : "Free");
            embed.AddField("Ranks", server.Ranks.Count);
            embed.AddField("Registered Users", server.UserList.Count);
            embed.AddField("Register Message", server.Registermessage);
            if (server.RegisterRole != 0)
                try
                {
                    var r = Context.Guild.GetRole(server.RegisterRole);
                    embed.AddField("Registration Role", r.Name);
                }
                catch
                {
                    //
                }
            else
                embed.AddField("Registration Role", "Everyone");
            if (server.AdminRole != 0)
                try
                {
                    var r = Context.Guild.GetRole(server.AdminRole);
                    embed.AddField("Bot Admin Role", r.Name);
                }
                catch
                {
                    //
                }

            embed.AddField("-----BOT STATS-----", "ELO BOT STATISTICS");
            embed.AddField("Total Servers", (Context.Client as DiscordSocketClient).Guilds.Count);
            embed.AddField("Total Users", (Context.Client as DiscordSocketClient).Guilds.Sum(x => x.MemberCount));
            embed.AddField("Total Channels", (Context.Client as DiscordSocketClient).Guilds.Sum(x => x.Channels.Count));
            embed.AddField("Total Registered", ServerList.LoadFull().Serverlist.Sum(x => x.UserList.Count));
            embed.AddField("Uptime",
                (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss"));
            embed.AddField("Heap Size",
                Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString(CultureInfo.InvariantCulture));
            await ReplyAsync("", false, embed.Build());
        }
    }
}