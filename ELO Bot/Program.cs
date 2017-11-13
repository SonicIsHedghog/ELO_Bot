using System;
using System.Collections.Generic;
using System.IO;
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
        private CommandHandler _handler;
        public DiscordSocketClient Client;

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

            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });

            var token = Config.Load().Token;

            try
            {
                await Client.LoginAsync(TokenType.Bot, token);
                await Client.StartAsync();
            }
            catch (Exception e)
            {
                Log.Information($"----------" +
                                $"\n{e}\n" +
                                $"----------\n" +
                                $"Token Rejected.\n" +
                                $"----------");
            }

            var serversave = File.ReadAllText(ServerList.EloFile);
            ServerList.Serverlist = JsonConvert.DeserializeObject<List<ServerList.Server>>(serversave);

            var serviceProvider = ConfigureServices();
            _handler = new CommandHandler(serviceProvider);
            await _handler.ConfigureAsync();

            Client.Log += Client_Log;
            await Task.Delay(-1);
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

        private static Task Client_Log(LogMessage arg)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            Log.Information(arg.ToString());
            return Task.CompletedTask;
        }
    }
}