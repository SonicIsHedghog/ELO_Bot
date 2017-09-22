using System;
using System.Collections.Generic;
using System.IO;
using Discord;
using Newtonsoft.Json;

namespace ELO_Bot
{
    public class ServerList
    {
        [JsonIgnore] public static string EloFile = Path.Combine(AppContext.BaseDirectory, "setup/serverlist.json");

        public List<Server> Serverlist { get; set; }

        public static Server Load(IGuild guild)
        {
            if (!File.Exists(EloFile))
                File.Create(EloFile).Dispose();
            var obj = JsonConvert.DeserializeObject<ServerList>(File.ReadAllText(EloFile));

            foreach (var server in obj.Serverlist)
                if (server.ServerId == guild.Id)
                    return server;

            var nullserver = new Server
            {
                ServerId = guild.Id,
                UserList = new List<Server.User>(),
                RegisterRole = 0,
                Registermessage = "Thankyou for Registering"
            };
            return nullserver;
        }

        public static ServerList LoadFull()
        {
            if (!File.Exists(EloFile))
                File.Create(EloFile).Dispose();
            var obj = JsonConvert.DeserializeObject<ServerList>(File.ReadAllText(EloFile));

            return obj;
        }

        public static void Saveserver(Server serverconfig)
        {
            var file = JsonConvert.DeserializeObject<ServerList>(File.ReadAllText(EloFile));
            foreach (var server in file.Serverlist)
                if (server.ServerId == serverconfig.ServerId)
                {
                    file.Serverlist.Remove(server);
                    break;
                }
            file.Serverlist.Add(serverconfig);
            var output = JsonConvert.SerializeObject(file, Formatting.Indented);
            File.WriteAllText(EloFile, output);
        }

        public class Server
        {
            public bool IsPremium { get; set; } = false;
            public string PremiumKey { get; set; } = "";

            public ulong ServerId { get; set; }
            public ulong RegisterRole { get; set; }
            public List<Ranking> Ranks { get; set; } = new List<Ranking>();
            public ulong AdminRole { get; set; } = 0;
            public string Registermessage { get; set; } = "Thankyou for Registering";
            public List<User> UserList { get; set; } = new List<User>();
            public ulong AnnouncementsChannel { get; set; } = 0;
            public int Winamount { get; set; } = 10;
            public int Lossamount { get; set; } = 5;

            public List<Q> Queue { get; set; } = new List<Q>();
            public List<PreviouMatches> Gamelist { get; set; } = new List<PreviouMatches>();



            public List<Ban> Bans { get; set; } = new List<Ban>();

            public class Ban
            {
                public ulong UserId;
                public DateTime Time;
            }

            public class Q
            {
                public List<ulong> Users { get; set; } = new List<ulong>();
                public ulong ChannelId { get; set; }
                public int UserLimit { get; set; } = 10;
                public string ChannelGametype { get; set; } = "Unknown";
                public int Games { get; set; } = 0;
                public List<string> Maps { get; set; } = new List<string>();

                public bool Captains { get; set; } = false;
                public bool IsPickingTeams { get; set; } = false;

                public ulong T1Captain { get; set; }
                public ulong T2Captain { get; set; }
                public List<ulong> Team1 { get; set; } = new List<ulong>();
                public List<ulong> Team2 { get; set; } = new List<ulong>();
            }

            public class PreviouMatches
            {
                public int GameNumber { get; set; }
                public ulong LobbyId { get; set; }
                public List<ulong> Team1 { get; set; } = new List<ulong>();
                public List<ulong> Team2 { get; set; } = new List<ulong>();
            }


            public class Ranking
            {
                public ulong RoleId { get; set; }
                public int Points { get; set; }
            }


            public class User
            {
                public string Username { get; set; }
                public ulong UserId { get; set; }
                public int Points { get; set; }
                public int Wins { get; set; }
                public int Losses { get; set; }
            }
        }
    }
}