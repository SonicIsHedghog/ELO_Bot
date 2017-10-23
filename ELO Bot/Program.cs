using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;

namespace ELO_Bot
{
    public class Program
    {
        public static int Messages;
        public static int Commands;
        private CommandHandler _handler;
        public DiscordSocketClient Client;
        public static List<string> Keys { get; set; }

        public static void Main(string[] args)
        {
            new Program().Start().GetAwaiter().GetResult();
        }

        public async Task Start()
        {
            Console.Title = "ELO Bot";

            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "setup/")))
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "setup/"));

            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "setup/backups/")))
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "setup/backups/"));

            Config.CheckExistence();
            var token = Config.Load().Token;


            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });

            try
            {
                await Client.LoginAsync(TokenType.Bot, token);
                await Client.StartAsync();
            }
            catch (Exception e)
            {
                Log.Information("Token was rejected by Discord (Invalid Token or Connection Error)\n" +
                                $"{e}");
            }


            var serviceProvider = ConfigureServices();
            _handler = new CommandHandler(serviceProvider);
            await _handler.ConfigureAsync();

            Client.Log += Client_Log;
            Client.Ready += Client_Ready;
            await Task.Delay(-1);
        }

        private static Task Client_Log(LogMessage arg)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            Log.Information(arg.ToString());
            return Task.CompletedTask;
        }

        private async Task Client_Ready()
        {
            var application = await Client.GetApplicationInfoAsync();
            Log.Information(
                $"Invite: https://discordapp.com/oauth2/authorize?client_id={application.Id}&scope=bot&permissions=2146958591");
            await Client.SetGameAsync($"{Config.Load().Prefix}register");

            var k = JsonConvert.DeserializeObject<List<string>>(
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "setup/keys.json")));
            if (k.Count > 0)
                Keys = k;
        }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection()
                .AddSingleton(Client)
                .AddSingleton(new InteractiveService(Client))
                .AddSingleton(new CommandService(
                    new CommandServiceConfig {CaseSensitiveCommands = false, ThrowOnError = false}));

            return services.BuildServiceProvider();
        }


        public class CommandHandler
        {
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
            }

            private static async Task _client_UserUpdated(SocketUser userBefore, SocketUser userAfter)
            {
                if (userBefore.Status != userAfter.Status)
                    if (userAfter.Status == UserStatus.Idle || userAfter.Status == UserStatus.Offline)
                    {
                        var guild = ((IGuildUser) userBefore).Guild;
                        var server = ServerList.Load(guild);
                        if (server.Autoremove && userAfter.Status == UserStatus.Idle)
                            foreach (var userqueue in server.Queue.Where(x => x.Users.Contains(userAfter.Id)))
                                try
                                {
                                    if (userqueue.IsPickingTeams)
                                    {
                                        var channel = await guild.GetChannelAsync(userqueue.ChannelId);
                                        await ((ITextChannel) channel).SendMessageAsync(
                                            $"{userAfter.Mention} has gone idle, as this channel is currently picking teams they were unable to be replaced in the queue\n" +
                                            "if they are inactive it is recommended that someone use the command `=subfor <@user>` to replace them\n" +
                                            "if they were already chosen for a team then you can use `=replace <@user>` after both teams are finished being picked.");
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
                            foreach (var userqueue in server.Queue.Where(x => x.Users.Contains(userAfter.Id)))
                                try
                                {
                                    if (userqueue.IsPickingTeams)
                                    {
                                        var channel = await guild.GetChannelAsync(userqueue.ChannelId);
                                        await ((ITextChannel) channel).SendMessageAsync(
                                            $"{userAfter.Mention} has gone offline, as this channel is currently picking teams they were unable to be replaced in the queue\n" +
                                            "if they are inactive it is recommended that someone use the command `=subfor <@user>` to replace them\n" +
                                            "if they were already chosen for a team then you can use `=replace <@user>` after both teams are finished being picked.");
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
                        ServerList.Saveserver(server);
                    }
            }

            private static async Task _client_JoinedGuild(SocketGuild guild)
            {
                var embed = new EmbedBuilder();
                embed.AddField("ELO Bot",
                    $"Hi there, I am ELO Bot. Type `{Config.Load().Prefix}help` to see a list of my commands and type `{Config.Load().Prefix}register <name>` to get started");
                embed.WithColor(Color.Blue);
                embed.AddField("Developed By PassiveModding", "Support Server: https://discord.gg/n2Vs38n \n" +
                                                              "Patreon: https://www.patreon.com/passivebot");
                try
                {
                    await guild.DefaultChannel.SendMessageAsync("", false, embed.Build());
                }
                catch
                {
                    //
                }
            }


            public async Task DoCommand(SocketMessage parameterMessage)
            {
                Messages++;
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

                    embed.AddField($"ERROR {result.Error.ToString().ToUpper()}", $"__Command:__ \n{context.Message}\n" +
                                                                                 $"__Error:__ \n**{result.ErrorReason}**\n\n" +
                                                                                 $"To report this error, please type: `{Config.Load().Prefix}report <errormessage>`");

                    embed.Color = Color.Red;
                    await context.Channel.SendMessageAsync("", false, embed.Build());
                    Log.Logger = new LoggerConfiguration()
                        .WriteTo.Console()
                        .CreateLogger();
                    Log.Error($"{message.Content} || {message.Author}");
                }
                else
                {
                    Log.Logger = new LoggerConfiguration()
                        .WriteTo.Console()
                        .CreateLogger();
                    Log.Information($"{message.Content} || {message.Author}");
                    Commands++;
                }

                if (Commands % 50 == 0)
                {
                    var backupfile = Path.Combine(AppContext.BaseDirectory,
                        $"setup/backups/{DateTime.UtcNow:dd-MM-yy HH.mm.ss}.txt");
                    File.WriteAllText(backupfile, File.ReadAllText(ServerList.EloFile));
                }
            }

            public async Task ConfigureAsync()
            {
                await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
            }
        }
    }
}