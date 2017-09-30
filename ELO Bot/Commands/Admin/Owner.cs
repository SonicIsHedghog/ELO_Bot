using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Newtonsoft.Json;

namespace ELO_Bot.Commands.Admin
{
    [RequireOwner]
    public class Owner : ModuleBase
    {
        [Command("addpremium")]
        [Summary("addpremium <KEYS>")]
        [Remarks("Add a fucking load of premium keys")]
        public async Task Addpremium(params string[] keys)
        {
            try
            {
                var i = 0;
                var duplicates = "Dupes:\n";
                if (Program.Keys == null)
                {
                    Program.Keys = keys.ToList();
                    await ReplyAsync("list replaced.");
                    var obj1 = JsonConvert.SerializeObject(Program.Keys, Formatting.Indented);
                    File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "setup/keys.json"), obj1);
                    return;
                }
                foreach (var key in keys)
                {
                    var dupe = false;
                    foreach (var k in Program.Keys)
                        if (k == key)
                            dupe = true;
                    if (!dupe)
                    {
                        i++;
                        Program.Keys.Add(key); //NO DUPES
                    }
                    else
                    {
                        duplicates += $"{key}\n";
                    }
                }
                await ReplyAsync($"{keys.Length} Supplied\n" +
                                 $"{i} Added\n" +
                                 $"{duplicates}");
                var obj = JsonConvert.SerializeObject(Program.Keys, Formatting.Indented);
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "setup/keys.json"), obj);
            }
            catch (Exception e)
            {
                await ReplyAsync(e.ToString());
            }
        }

        [Command("botrename")]
        [Summary("botrename")]
        public async Task Help([Remainder] string name)
        {
            await Context.Client.CurrentUser.ModifyAsync(x => { x.Username = name; });
        }
    }
}