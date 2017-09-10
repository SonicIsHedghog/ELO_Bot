using System;
using System.Collections.Generic;
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

        [Command("help")]
        [Summary("help")]
        [Remarks("all help commands")]
        public async Task HelpAsync(string modulearg = null)
        {
            var embed = new EmbedBuilder
            {
                Color = new Color(114, 137, 218),
                Title = $"ELO BOT | Commands | Prefix: {Config.Load().Prefix}"
            };
            if (modulearg == null) //ShortHelp
            {
                foreach (var module in _service.Modules)
                {
                    var list = module.Commands.Select(command => command.Name).ToList();
                    if (module.Commands.Count > 0)
                        embed.AddField(x =>
                        {
                            x.Name = module.Name;
                            x.Value = string.Join(", ", list);
                        });
                }
                embed.AddField("**NOTE**",
                    $"You can also see modules in more detail using `=help <modulename>`");

                embed.AddField("Developed By PassiveModding", "Support Server: https://discord.gg/n2Vs38n \n" +
                                                              "Patreon: https://www.patreon.com/passivebot  \n" +
                                                              "Invite the BOT: https://goo.gl/mbfnjj");
            }
            else
            {
                foreach (var module in _service.Modules)
                    if (string.Equals(module.Name, modulearg, StringComparison.CurrentCultureIgnoreCase))
                    {
                        var list = new List<string>();
                        foreach (var command in module.Commands)
                            list.Add(
                                $"{Config.Load().Prefix}{command.Summary} - {command.Remarks}");
                        embed.AddField(module.Name, string.Join("\n", list));
                    }
                if (embed.Fields.Count == 0)
                {
                    embed.AddField("Error", $"{modulearg} is not a module");
                    var list = _service.Modules.Select(module => module.Name).ToList();
                    embed.AddField("Modules", string.Join("\n", list));
                }
            }
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