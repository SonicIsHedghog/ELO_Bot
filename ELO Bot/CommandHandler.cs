using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace ELO_Bot
{
    public class CommandHandler
    {
        public static int Commands;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        public IServiceProvider Provider;

        public CommandHandler(IServiceProvider provider)
        {
            Provider = provider;
            _client = Provider.GetService<DiscordSocketClient>();
            _commands = new CommandService();

            _client.MessageReceived += DoCommand;
            _client.JoinedGuild += _client_JoinedGuild;
            _client.GuildMemberUpdated += _client_UserUpdated;
            _client.Ready += Client_Ready;
        }

        public static List<string> Keys { get; set; }
        public static List<ulong> VerifiedUsers { get; set; }

        private static async Task _client_UserUpdated(SocketUser userBefore, SocketUser userAfter)
        {
            if (userBefore.Status != userAfter.Status)
                if (userAfter.Status == UserStatus.Idle || userAfter.Status == UserStatus.Offline)
                {
                    var guild = ((IGuildUser) userBefore).Guild;
                    var server =
                        Servers.ServerList.FirstOrDefault(x => x.ServerId == ((SocketGuildUser) userAfter).Guild.Id);
                    if (server != null && server.Autoremove && userAfter.Status == UserStatus.Idle)
                        foreach (var userqueue in server.Queue.Where(x => x.Users.Contains(userAfter.Id)))
                            try
                            {
                                if (userqueue.IsPickingTeams)
                                {
                                    var channel = await guild.GetChannelAsync(userqueue.ChannelId);
                                    await ((ITextChannel) channel).SendMessageAsync(
                                        $"{userAfter.Mention} has gone idle, as this channel is currently picking teams they were unable to be replaced in the queue\n" +
                                        $"if they are inactive it is recommended that someone use the command `=subfor <@user>` to replace them\n" +
                                        $"if they were already chosen for a team then you can use `=replace <@user>` after both teams are finished being picked.");
                                }
                                else
                                {
                                    userqueue.Users.Remove(userAfter.Id);
                                    var channel = await guild.GetChannelAsync(userqueue.ChannelId);
                                    await ((ITextChannel) channel).SendMessageAsync(
                                        $"{userAfter.Mention} has gone idle and has been removed from this channel's queue");
                                }
                            }
                            catch
                            {
                                //
                            }
                    else if (userAfter.Status == UserStatus.Offline)
                        if (server != null)
                            foreach (var userqueue in server.Queue.Where(x => x.Users.Contains(userAfter.Id)))
                                try
                                {
                                    if (userqueue.IsPickingTeams)
                                    {
                                        var channel = await guild.GetChannelAsync(userqueue.ChannelId);
                                        await ((ITextChannel) channel).SendMessageAsync(
                                            $"{userAfter.Mention} has gone offline, as this channel is currently picking teams they were unable to be replaced in the queue\n" +
                                            $"if they are inactive it is recommended that someone use the command `=subfor <@user>` to replace them\n" +
                                            $"if they were already chosen for a team then you can use `=replace <@user>` after both teams are finished being picked.");
                                    }
                                    else
                                    {
                                        userqueue.Users.Remove(userAfter.Id);
                                        var channel = await guild.GetChannelAsync(userqueue.ChannelId);
                                        await ((ITextChannel) channel).SendMessageAsync(
                                            $"{userAfter.Mention} has gone offline and has been removed from this channel's queue");
                                    }
                                }
                                catch
                                {
                                    //
                                }
                }
        }

        private static async Task _client_JoinedGuild(SocketGuild guild)
        {
            var embed = new EmbedBuilder();
            embed.AddField("ELO Bot",
                $"Hi there, I am ELO Bot. Type `{Config.Load().Prefix}help` to see a list of my commands and type `{Config.Load().Prefix}register <name>` to get started");
            embed.WithColor(Color.Blue);
            embed.AddField("Developed By PassiveModding", "Support Server: https://discord.gg/n2Vs38n ");
            try
            {
                await guild.DefaultChannel.SendMessageAsync("", false, embed.Build());
            }
            catch
            {
                //
            }

            if (Servers.ServerList.All(x => x.ServerId != guild.Id))
            {
                var server = new Servers.Server
                {
                    ServerId = guild.Id,
                    UserList = new List<Servers.Server.User>()
                };

                Servers.ServerList.Add(server);
            }
        }

        private async Task Client_Ready()
        {
            var application = await _client.GetApplicationInfoAsync();
            Console.WriteLine(
                $"Invite: https://discordapp.com/oauth2/authorize?client_id={application.Id}&scope=bot&permissions=2146958591");
            //await _client.SetGameAsync($"{Config.Load().Prefix}register");


            var k = JsonConvert.DeserializeObject<List<string>>(
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "setup/keys.json")));
            if (k.Count > 0)
                Keys = k;

            var backupfile = Path.Combine(AppContext.BaseDirectory,
                "setup/serverlist.json");
            var jsonbackup = JsonConvert.SerializeObject(Servers.ServerList);
            File.WriteAllText(backupfile, jsonbackup);

            var verif = JsonConvert.DeserializeObject<List<ulong>>(
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "setup/verified.json")));
            if (verif.Count > 0)
            {
                VerifiedUsers = verif;
                foreach (var user in VerifiedUsers)
                {
                    foreach (var server in _client.Guilds)
                    {
                        try
                        {
                            var patreon = server.GetUser(user);
                            if (patreon != null)
                            {
                                var serverobject = Servers.ServerList.First(x => x.ServerId == server.Id);
                                var userprofile = serverobject.UserList.First(x => x.UserId == user);
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

        public async Task DoCommand(SocketMessage parameterMessage)
        {
            if (!(parameterMessage is SocketUserMessage message)) return;
            var argPos = 0;
            var context = new SocketCommandContext(_client, message); //new CommandContext(_client, message);

            if (context.User.IsBot)
                return;

            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) ||
                  message.HasStringPrefix(Config.Load().Prefix, ref argPos))) return;

            var result = await _commands.ExecuteAsync(context, argPos, Provider);

            var commandsuccess = result.IsSuccess;


            if (!commandsuccess)
            {
                var embed = new EmbedBuilder();

                foreach (var module in _commands.Modules)
                foreach (var command in module.Commands)
                    if (context.Message.Content.ToLower()
                        .StartsWith($"{Config.Load().Prefix}{command.Name} ".ToLower()))
                    {
                        embed.AddField("COMMAND INFO", $"Name: `{Config.Load().Prefix}{command.Summary}`\n" +
                                                       $"Info: {command.Remarks}");
                        break;
                    }

                embed.Title = $"ERROR {result.Error}";

                embed.AddField("Error Type", $"#: **{result.ErrorReason}**");
                embed.AddField("Command", $"#: **{context.Message}**");
                embed.AddField("Report",
                    $"#: To report this error, please type: `{Config.Load().Prefix}BugReport <errormessage>`");

                embed.Color = Color.Red;
                await context.Channel.SendMessageAsync("", false, embed.Build());
                await Logger.In3Error(context.Message.Content, context.Guild.Name, context.Channel.Name,
                    context.User.Username);
            }
            else
            {
                await Logger.In3(context.Message.Content, context.Guild.Name, context.Channel.Name,
                    context.User.Username);

                var rnd = new Random().Next(0, 100);
                if (rnd == 1)
                    try
                    {
                        var embed = new EmbedBuilder
                        {
                            Title = "Consider supporting this project by Joining the Patreon!",
                            Url = "http://patreon.com/passivebot"
                        };
                        await context.Channel.SendMessageAsync("", false, embed.Build());
                    }
                    catch
                    {
                        //
                    }

                Commands++;
            }

            if (Commands % 100 == 0)
            {
                var backupfile = Path.Combine(AppContext.BaseDirectory,
                    $"setup/backups/{DateTime.UtcNow:dd-MM-yy HH.mm.ss}.txt");


                var jsonbackup = JsonConvert.SerializeObject(Servers.ServerList);
                File.WriteAllText(backupfile, jsonbackup);
                File.WriteAllText(Servers.EloFile, jsonbackup);
            }
        }

        public async Task ConfigureAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }
    }
}