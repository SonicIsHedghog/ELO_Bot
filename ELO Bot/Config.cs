using System;
using System.IO;
using Newtonsoft.Json;
using Serilog;

namespace ELO_Bot
{
    public class Config
    {
        [JsonIgnore] public static readonly string Appdir = AppContext.BaseDirectory;


        public static string ConfigPath = Path.Combine(AppContext.BaseDirectory, "setup/config.json");

        public string Prefix { get; set; } = "=";
        public string Token { get; set; } = "Token";
        public bool AutoRun { get; set; }
        public string DiscordInvite { get; set; } = "https://discordapp.com/invite/ZKXqt2a";

        public void Save(string dir = "setup/config.json")
        {
            var file = Path.Combine(Appdir, dir);
            File.WriteAllText(file, ToJson());
        }

        public static Config Load(string dir = "setup/config.json")
        {
            var file = Path.Combine(Appdir, dir);
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(file));
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static void CheckExistence()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            bool auto;
            try
            {
                auto = Load().AutoRun;
            }
            catch
            {
                auto = false;
            }
            if (auto)
            {
            }
            else
            {
                Log.Information("Run (Y for run, N for setup Config)");

                Log.Information("Y or N: ");
                var res = Console.ReadLine();
                if (res == "N" || res == "n")
                    File.Delete("setup/config.json");

                if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "setup/")))
                    Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "setup/"));
            }


            if (!File.Exists(ConfigPath))
            {
                var cfg = new Config();

                Log.Information(
                    @"Please enter a prefix for the bot eg. '+' (do not include the '' outside of the prefix)");
                Console.Write("Prefix: ");
                cfg.Prefix = Console.ReadLine();

                Log.Information(
                    @"After you input your token, a config will be generated at 'setup/config.json'");
                Console.Write("Token: ");
                cfg.Token = Console.ReadLine();

                Log.Information("Would you like to AutoRun the bot from now on? Y/N");
                var type2 = Console.ReadKey();
                cfg.AutoRun = type2.KeyChar.ToString().ToLower() == "y";

                cfg.Save();
            }

            Log.Information("Config Loaded!");
            Log.Information($"Prefix: {Load().Prefix}");
            Log.Information($"Token Length: {Load().Token.Length} (should be 59)");
            Log.Information($"Autorun: {Load().AutoRun}");
        }
    }
}