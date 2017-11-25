using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Newtonsoft.Json;

namespace ELO_Bot.Commands
{
    public class StatsLookup : InteractiveBase
    {
        [Command("R6User")]
        [Summary("R6User <username>")]
        [Remarks("Get a r6s user profile")]
        public async Task R6User([Remainder] string username)
        {
                var url = $"https://api.r6stats.com/api/v1/players/{username}?platform=uplay";



                using (var client = new WebClient())
                {
                    client.Encoding = Encoding.Unicode;
                    client.DownloadFile($"{url}", Path.Combine(AppContext.BaseDirectory, $"{Context.Message.Id}.txt"));
                }

                var user =
                    JsonConvert.DeserializeObject<R6Profile.RootObject>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
                        $"{Context.Message.Id}.txt")));

                //await ReplyAsync(JsonConvert.SerializeObject(user));

                var pages = new List<string>();
                var p = user.player;

                if (user.player.stats.ranked.has_played)
                    pages.Add($"**Ranked Stats**\n" +
                              $"Kills: {p.stats.ranked.kills}\n" +
                              $"Deaths: {p.stats.ranked.deaths}\n" +
                              $"K/D: {p.stats.ranked.kd}\n" +
                              $"Wins: {p.stats.ranked.wins}\n" +
                              $"Losses: {p.stats.ranked.losses}\n" +
                              $"W/L: {p.stats.ranked.wlr}\n" +
                              $"Playtime (H): {TimeSpan.FromSeconds(p.stats.ranked.playtime).TotalHours}");

                if (user.player.stats.casual.has_played)
                    pages.Add($"**Casual Stats**\n" +
                              $"Kills: {p.stats.casual.kills}\n" +
                              $"Deaths: {p.stats.casual.deaths}\n" +
                              $"K/D: {p.stats.casual.kd}\n" +
                              $"Wins: {p.stats.casual.wins}\n" +
                              $"Losses: {p.stats.casual.losses}\n" +
                              $"W/L: {p.stats.casual.wlr}\n" +
                              $"Playtime (H): {TimeSpan.FromSeconds(p.stats.casual.playtime).TotalHours}");

                pages.Add($"**Misc Stats**\n\n" +
                          $"Assists: {p.stats.overall.assists}\n" +
                          $"Barricades Built: {p.stats.overall.barricades_built}\n" +
                          $"Bullets Fired: {p.stats.overall.bullets_fired}\n" +
                          $"Bullets Hit: {p.stats.overall.bullets_hit}\n" +
                          $"Headshots: {p.stats.overall.headshots}\n" +
                          $"Melee Kills: {p.stats.overall.melee_kills}\n" +
                          $"Penetration Kills: {p.stats.overall.penetration_kills}\n" +
                          $"Revives: {p.stats.overall.revives}\n" +
                          $"Reinforcements Deployed: {p.stats.overall.reinforcements_deployed}\n" +
                          $"Steps Moved: {p.stats.overall.steps_moved}\n" +
                          $"Suicides: {p.stats.overall.suicides}");

                var msg = new PaginatedMessage
                {
                    Title = $"R6s Profile of {username}",
                    Pages = pages,
                    Color = new Color(114, 137, 218)
                };

                await PagedReplyAsync(msg);

        }
    }
}
