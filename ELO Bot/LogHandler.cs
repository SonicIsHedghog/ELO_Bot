using System;
using System.Drawing;
using System.Threading.Tasks;
using Serilog;

namespace ELO_Bot
{
    public class Logger
    {
        public static Task In3(string command, string server, string channel, string user)
        {
            command = $"{command}                                 ".Substring(0, 20).Replace("\n", " ");
            server = $"{server}                                   ".Substring(0, 15);
            channel = $"{channel}                                 ".Substring(0, 15);
            user = $"{user}            ".Substring(0, 15);

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            Log.Information($"{command} | S: {server} | C: {channel} | U: {user}");

            return Task.CompletedTask;
        }

        public static Task In3Error(string command, string server, string channel, string user)
        {
            command = $"{command}                                 ".Substring(0, 20).Replace("\n", " ");
            server = $"{server}                                   ".Substring(0, 15);
            channel = $"{channel}                                 ".Substring(0, 15);
            user = $"{user}            ".Substring(0, 15);

            Log.Error(
                $"{command} | S: {server} | C: {channel} | U: {user}");
            return Task.CompletedTask;
        }

        public static Task Debug(string message)
        {
            message = message.Replace("\n", " ");
            var msg = message.Substring(21, message.Length - 21);

            Console.WriteLine($"{DateTime.Now:dd/MM/yyyy hh:mm:ss tt} [Debug] PassiveBOT           | {msg}",
                Color.GreenYellow);
            return Task.CompletedTask;
        }
    }
}