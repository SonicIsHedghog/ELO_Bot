using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;

namespace ELO_Bot
{
    public class Program
    {
        private CommandHandler _handler;
        public DiscordSocketClient Client;
        public static List<string> Keys { get; set; }

        public static void Main(string[] args)
        {
            new Program().Start().GetAwaiter().GetResult();
        }

        public async Task Start()
        {
            Console.Title = "BlackBOT";

            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "setup/")))
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "setup/"));

            Config.CheckExistence();
            var token = Config.Load().Token;


            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 500
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

            Client.Ready += Client_Ready;
            await Task.Delay(-1);
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
                var message = parameterMessage as SocketUserMessage;
                if (message == null) return;
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
                    embed.AddField("ERROR", $"Command: {context.Message}\n" +
                                            $"Error: {result.ErrorReason}");
                    embed.Color = Color.Red;
                    var x = await context.Channel.SendMessageAsync("", false, embed.Build());

                    await Task.Delay(5000);
                    await x.DeleteAsync();
                    await context.Message.DeleteAsync();
                }
                else
                {
                    Log.Logger = new LoggerConfiguration()
                        .WriteTo.Console()
                        .CreateLogger();
                    Log.Information($"{message.Content} || {message.Author}");
                }
            }

            public async Task ConfigureAsync()
            {
                await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
            }
        }
    }
}