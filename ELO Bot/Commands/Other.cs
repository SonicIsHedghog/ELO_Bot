using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using ELO_Bot.Preconditions;
using Newtonsoft.Json.Linq;

namespace ELO_Bot.Commands
{
    [RequireContext(ContextType.Guild)]
    [CheckBlacklist]
    public class Other : InteractiveBase
    {
        private readonly CommandService _service;

        public Other(CommandService service)
        {
            _service = service;
        }

        /// <summary>
        ///     lists all administrator commands
        /// </summary>
        /// <param name="modulearg"></param>
        /// <returns></returns>
        [Command("adminhelp")]
        [Summary("adminhelp")]
        [Remarks("adminall help commands")]
        public async Task AHelpAsync([Remainder] string modulearg = null)
        {
            var pages = new List<string>();

            var modules = _service.Modules.Select(x => x.Name).Where(x =>
                x.ToLower() != "other" && x.ToLower() != "matchmaking" &&
                x.ToLower() != "user" && x.ToLower() != "owner");
            var modulesector = $"**Modules**\n" +
                               $"{string.Join("\n", modules)}\n\n" +
                               $"Press the arrow buttons to go through each module and its commands";
            pages.Add(modulesector);

            foreach (var module in _service.Modules)
                if (module.Name.ToLower() != "other" && module.Name.ToLower() != "matchmaking" &&
                    module.Name.ToLower() != "user" && module.Name.ToLower() != "owner")
                {
                    var list = module.Commands
                        .Select(command => $"`{Config.Load().Prefix}{command.Summary}` - {command.Remarks}").ToList();
                    if (module.Commands.Count > 0)
                    {
                        var toadd = $"**{module.Name}**\n" +
                                    $"{string.Join("\n", list)}";
                        pages.Add(toadd);
                    }
                }

            pages.Add("**Links**\n" +
                      "[Support Server](https://discord.gg/n2Vs38n)\n" +
                      "[Patreon](https://www.patreon.com/passivebot)\n" +
                      "[Invite the BOT](https://goo.gl/mbfnjj)");

            var msg = new PaginatedMessage
            {
                Title = $"ELO BOT | Commands | Prefix: {Config.Load().Prefix}",
                Pages = pages,
                Color = new Color(114, 137, 218)
            };


            await PagedReplyAsync(msg);
        }

        /// <summary>
        ///     lists all regular commands
        /// </summary>
        /// <param name="modulearg"></param>
        /// <returns></returns>
        [Command("help")]
        [Summary("help")]
        [Remarks("all help commands")]
        public async Task HelpAsync([Remainder] string modulearg = null)
        {
            var pages = new List<string>();

            var modules = _service.Modules.Select(x => x.Name).Where(x =>
                x.ToLower() == "other" || x.ToLower() == "matchmaking" ||
                x.ToLower() == "user");
            var modulesector = $"**Modules**\n" +
                               $"{string.Join("\n", modules)}\n\n" +
                               $"Press the arrow buttons to go through each module and its commands";
            pages.Add(modulesector);

            foreach (var module in _service.Modules)
                if (module.Name.ToLower() == "other" || module.Name.ToLower() == "matchmaking" ||
                    module.Name.ToLower() == "user")
                {
                    var list = module.Commands
                        .Select(command => $"`{Config.Load().Prefix}{command.Summary}` - {command.Remarks}").ToList();
                    if (module.Commands.Count > 0)
                        pages.Add($"**{module.Name}**\n" +
                                  $"{string.Join("\n", list)}");
                }

            pages.Add("**Links**\n" +
                      "[Support Server](https://discord.gg/n2Vs38n)\n" +
                      "[Patreon](https://www.patreon.com/passivebot)\n" +
                      "[Invite the BOT](https://goo.gl/mbfnjj)");

            var msg = new PaginatedMessage
            {
                Title = $"ELO BOT | Commands | Prefix: {Config.Load().Prefix}",
                Pages = pages,
                Color = new Color(114, 137, 218)
            };


            await PagedReplyAsync(msg);
        }

        /// <summary>
        ///     information about the bot on github as well as other stuff about usercounts etc.
        /// </summary>
        /// <returns></returns>
        [Command("Info")]
        [Summary("Info")]
        [Remarks("Bot Info and Stats")]
        public async Task Info()
        {
            var client = Context.Client;
            var hClient = new HttpClient();
            string changes;
            hClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
            using (var response =
                await hClient.GetAsync("https://api.github.com/repos/PassiveModding/ELO_Bot/commits"))
            {
                if (!response.IsSuccessStatusCode)
                {
                    changes = "There was an error fetching the latest changes.";
                }
                else
                {
                    dynamic result = JArray.Parse(await response.Content.ReadAsStringAsync());
                    changes =
                        $"[{((string) result[0].sha).Substring(0, 7)}]({result[0].html_url}) {result[0].commit.message}\n" +
                        $"[{((string) result[1].sha).Substring(0, 7)}]({result[1].html_url}) {result[1].commit.message}\n" +
                        $"[{((string) result[2].sha).Substring(0, 7)}]({result[2].html_url}) {result[2].commit.message}";
                }
                response.Dispose();
            }
            var embed = new EmbedBuilder();

            embed.WithAuthor(x =>
            {
                x.IconUrl = Context.Client.CurrentUser.GetAvatarUrl();
                x.Name = $"{client?.CurrentUser.Username}'s Official Invite";
                if (client != null)
                    x.Url =
                        $"https://discordapp.com/oauth2/authorize?client_id={client.CurrentUser.Id}&scope=bot&permissions=2146958591";
            });
            embed.AddField("Changes", changes);
            if (client != null)
            {
                embed.AddField("Members",
                    $"Bot: {client.Guilds.Sum(x => x.Users.Count(z => z.IsBot))}\n" +
                    $"Human: {client.Guilds.Sum(x => x.Users.Count(z => !z.IsBot))}\n" +
                    $"Total: {client.Guilds.Sum(x => x.Users.Count)}", true);
                embed.AddField("Channels",
                    $"Text: {client.Guilds.Sum(x => x.TextChannels.Count)}\n" +
                    $"Voice: {client.Guilds.Sum(x => x.VoiceChannels.Count)}\n" +
                    $"Total: {client.Guilds.Sum(x => x.Channels.Count)}", true);
                embed.AddField("Guilds", $"{client.Guilds.Count}\n[Support Guild](https://discord.gg/ZKXqt2a)",
                    true);
            }
            embed.AddField(":space_invader:",
                $"Commands Ran: {CommandHandler.Commands}", true);
            embed.AddField(":hammer_pick:",
                $"Heap Size: {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2)} MB", true);
            embed.AddField(":beginner:", "Written by: [PassiveModding](https://github.com/PassiveModding)\n" +
                                         $"Discord.Net {DiscordConfig.Version}");

            await ReplyAsync("", embed: embed.Build());
        }

        /// <summary>
        ///     report an issue directly to the bot owner
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [Command("BugReport")]
        [Summary("BugReport <errormessage>")]
        [Remarks("an error or issue")]
        public async Task Report([Remainder] string message = null)
        {
            if (message == null)
            {
                await ReplyAsync("Please supply an error message");
                return;
            }
            var embed = new EmbedBuilder();

            embed.AddField("ERROR REPORT", $"From: {Context.User.Username}\n" +
                                           $"Server: {Context.Guild.Name}\n" +
                                           $"Channel: {Context.Channel.Name}\n" +
                                           $"Invite: {((SocketGuildChannel) Context.Channel).CreateInviteAsync(0).Result}\n" +
                                           "ERROR MESSAGE:\n" +
                                           $"{message}");

            //var m = await Context.Channel.GetMessagesAsync(10).Flatten();

            var e = await Context.Client.GetApplicationInfoAsync();
            await e.Owner.SendMessageAsync("", false, embed.Build());

            await ReplyAsync(
                "Thankyou! Your Error report has been sent to the bot owner, along with some information about you and this current server.");
        }
    }
}