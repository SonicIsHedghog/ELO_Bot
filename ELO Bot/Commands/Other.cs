using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;

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

        [Command("adminhelp")]
        [Summary("adminhelp")]
        [Remarks("adminall help commands")]
        public async Task AHelpAsync(string modulearg = null)
        {
            var embed = new EmbedBuilder
            {
                Color = new Color(114, 137, 218),
                Title = $"ELO BOT | Commands | Prefix: {Config.Load().Prefix}"
            };


            if (modulearg == null) //ShortHelp
            {
                foreach (var module in _service.Modules)
                    if (module.Name.ToLower() != "other" && module.Name.ToLower() != "matchmaking" &&
                        module.Name.ToLower() != "usercommands")
                    {
                        var list = module.Commands.Select(command => command.Name).ToList();
                        if (module.Commands.Count > 0)
                            embed.AddField(x =>
                            {
                                x.Name = $"{module.Name} Commands";
                                x.Value = string.Join(", ", list);
                            });
                    }
                embed.AddField("**NOTE**",
                    $"You can also see modules in more detail using `=help <modulename>`");

                embed.AddField("Developed By PassiveModding", "[Support Server](https://discord.gg/n2Vs38n)\n" +
                                                              "[Patreon](https://www.patreon.com/passivebot)\n" +
                                                              "[Invite the BOT](https://goo.gl/mbfnjj)");
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
                    if (module.Name.ToLower() == "other" || module.Name.ToLower() == "matchmaking" ||
                        module.Name.ToLower() == "usercommands")
                    {
                        var list = module.Commands.Select(command => command.Name).ToList();
                        if (module.Commands.Count > 0)
                            embed.AddField(x =>
                            {
                                x.Name = $"{module.Name} Commands";
                                x.Value = string.Join(", ", list);
                            });
                    }
                embed.AddField("**NOTE**",
                    $"You can also see modules in more detail using `=help <modulename>`\n" +
                    $"Admins can use `=adminhelp` for admin commands");

                embed.AddField("Developed By PassiveModding", "[Support Server](https://discord.gg/n2Vs38n)\n" +
                                                              "[Patreon](https://www.patreon.com/passivebot)\n" +
                                                              "[Invite the BOT](https://goo.gl/mbfnjj)");
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

        [Command("Info")]
        [Summary("Info")]
        [Remarks("Bot Info and Stats")]
        public async Task Info()
        {
            try
            {
                    var client = Context.Client as DiscordSocketClient;
                    var hClient = new HttpClient();
                    string changes;
                    hClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
                    using (var response = await hClient.GetAsync("https://api.github.com/repos/PassiveModding/ELO_Bot/commits"))
                    {
                        if (!response.IsSuccessStatusCode)
                            changes = "There was an error fetching the latest changes.";
                        else
                        {
                            dynamic result = JArray.Parse(await response.Content.ReadAsStringAsync());
                            changes =
                                $"[{((string)result[0].sha).Substring(0, 7)}]({result[0].html_url}) {result[0].commit.message}\n" +
                                $"[{((string)result[1].sha).Substring(0, 7)}]({result[1].html_url}) {result[1].commit.message}\n" +
                                $"[{((string)result[2].sha).Substring(0, 7)}]({result[2].html_url}) {result[2].commit.message}";
                        }
                        response.Dispose();
                    }
                    var embed = new EmbedBuilder();

                embed.WithAuthor(x =>
                {
                    x.IconUrl = Context.Client.CurrentUser.GetAvatarUrl();
                    x.Name = $"{client.CurrentUser.Username}'s Official Invite";
                    x.Url =
                        $"https://discordapp.com/oauth2/authorize?client_id={client.CurrentUser.Id}&scope=bot&permissions=2146958591";
                });
                    embed.AddField("Changes", changes);
                    embed.AddField("Members",
                        $"Bot: {client.Guilds.Sum(x => x.Users.Count(z => z.IsBot))}\n" +
                        $"Human: {client.Guilds.Sum(x => x.Users.Count(z => !z.IsBot))}\n" +
                        $"Total: {client.Guilds.Sum(x => x.Users.Count)}", true);
                    embed.AddField("Channels",
                        $"Text: {client.Guilds.Sum(x => x.TextChannels.Count)}\n" +
                        $"Voice: {client.Guilds.Sum(x => x.VoiceChannels.Count)}\n" +
                        $"Total: {client.Guilds.Sum(x => x.Channels.Count)}", true);
                    embed.AddField("Guilds", $"{client.Guilds.Count}\n[Support Guild](https://discord.gg/ZKXqt2a)", true);
                    embed.AddField(":space_invader:",
                        $"Commands Ran: {Program.Commands}\n" +
                        $"Messages Received: {Program.Messages}", true);
                    embed.AddField(":hammer_pick:",
                        $"Heap Size: {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2)} MB", true);
                    embed.AddField(":beginner:", $"Written by: [PassiveModding](https://github.com/PassiveModding)\n" +
                                                 $"Discord.Net {DiscordConfig.Version}");
                    await ReplyAsync("", embed: embed.Build());
            }
            catch (Exception e)
            {
                await ReplyAsync(e.ToString());
            }
            
        }

        /*
        [Command("Stats")]
        [Remarks("Stats and info about the bot & current server")]
        [Summary("Stats")]
        [CheckRegistered]
        public async Task Stats()
        {
            var server = ServerList.Load(Context.Guild);


            var announce = await Context.Guild.GetChannelAsync(server.AnnouncementsChannel);
            var admin = Context.Guild.GetRole(server.AdminRole);
            var winloss = $"{server.Winamount}/{server.Lossamount}";
            //var status = server.IsPremium;
            var register = $"{server.Registermessage}";
            var registerrole = Context.Guild.GetRole(server.RegisterRole);
            var games = server.Gamelist.Count;
            var lobbies = server.Queue.Count;
            var users = server.UserList.Count;
            var ranks = server.Ranks.Count;


            var embed = new EmbedBuilder();
            embed.AddField("Server Stats",
                $"Announcements Channel: {announce.Name}\n" +
                $"Admin Role: {admin.Name}\n" +
                $"Win/Loss Point Count: {winloss}\n" +
                $"Server Status:" + (server.IsPremium ? "Premium" : "Free") + "\n" +
                $"Register Message: {register}\n" +
                $"Register Role: {registerrole.Name}\n" +
                $"User Count: {users}" +
                $"Games Played: {games}\n" +
                $"Lobby Count: {lobbies}\n" +
                $"Ranks: {ranks}");

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
        }*/
    }
}