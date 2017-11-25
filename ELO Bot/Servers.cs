using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace ELO_Bot
{
    public class Servers
    {
        [JsonIgnore] public static string EloFile = Path.Combine(AppContext.BaseDirectory, "setup/serverlist.json");

        public static List<Server> ServerList = new List<Server>();

        /*public class DocumentStoreHolder
        {
            private static readonly Lazy<IDocumentStore> LazyStore = new Lazy<IDocumentStore>(CreateStore);

            public static IDocumentStore Store => LazyStore.Value;

            private static IDocumentStore CreateStore()
            {
                var store = new DocumentStore
                {
                    Url = "http://localhost:8080",
                    DefaultDatabase = "Servers"
                }.Initialize();

                return store;
            }
        }*/


        public class Server
        {
            public bool IsPremium { get; set; } = false;
            public string PremiumKey { get; set; } = "";

            public ulong ServerId { get; set; }
            public ulong RegisterRole { get; set; }
            public List<Ranking> Ranks { get; set; } = new List<Ranking>();
            public ulong AdminRole { get; set; } = 0;
            public ulong ModRole { get; set; } = 0;
            public string Registermessage { get; set; } = "Thankyou for Registering";
            public List<User> UserList { get; set; } = new List<User>();
            public ulong AnnouncementsChannel { get; set; } = 0;
            public int Winamount { get; set; } = 10;
            public int Lossamount { get; set; } = 5;
            public bool Autoremove { get; set; } = true;
            public List<string> CmdBlacklist { get; set; } = new List<string>();
            public bool BlockMultiQueueing { get; set; } = false;

            public List<Q> Queue { get; set; } = new List<Q>();
            public List<PreviouMatches> Gamelist { get; set; } = new List<PreviouMatches>();


            public List<Ban> Bans { get; set; } = new List<Ban>();

            public class Ban
            {
                public DateTime Time { get; set; }
                public ulong UserId { get; set; }
                public string Reason { get; set; } = null;
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

                public bool? Result { get; set; } = null;
                // result by default is null,
                //true represents team1
                //false represents team2
            }


            public class Ranking
            {
                public ulong RoleId { get; set; }
                public int Points { get; set; }
                public int WinModifier { get; set; } = 0;

                public int LossModifier { get; set; } = 0;
                //WinModifier and loss modifier
                //check if zero. If not, make adjustments to score
                //based on this per rank rather than per server.
            }


            public class User
            {
                public string Username { get; set; }
                public ulong UserId { get; set; }
                public int Points { get; set; } = 0;
                public int Wins { get; set; } = 0;
                public int Losses { get; set; } = 0;
            }
        }
    }
}