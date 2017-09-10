using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace ELO_Bot.Commands
{
    [RequireContext(ContextType.Guild)]
    public class Other : ModuleBase
    {
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
                if (module.Name.ToLower() != "usercommands" && module.Name.ToLower() != "matchmaking" &&
                    module.Name.ToLower() != "owner" && module.Name.ToLower() != "other")
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
                if (module.Name.ToLower() == "usercommands" || module.Name.ToLower() == "matchmaking" ||
                    module.Name.ToLower() == "other")
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
        [CheckRegistered]
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