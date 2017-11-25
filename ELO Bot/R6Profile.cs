using System;

namespace ELO_Bot
{
    public class R6Profile
    {
        public class Ranked
        {
            public bool has_played { get; set; }
            public int wins { get; set; }
            public int losses { get; set; }
            public double wlr { get; set; }
            public int kills { get; set; }
            public int deaths { get; set; }
            public double kd { get; set; }
            public int playtime { get; set; }
        }

        public class Casual
        {
            public bool has_played { get; set; }
            public int wins { get; set; }
            public int losses { get; set; }
            public double wlr { get; set; }
            public int kills { get; set; }
            public int deaths { get; set; }
            public double kd { get; set; }
            public double playtime { get; set; }
        }

        public class Overall
        {
            public int revives { get; set; }
            public int suicides { get; set; }
            public int reinforcements_deployed { get; set; }
            public int barricades_built { get; set; }
            public int steps_moved { get; set; }
            public int bullets_fired { get; set; }
            public int bullets_hit { get; set; }
            public int headshots { get; set; }
            public int melee_kills { get; set; }
            public int penetration_kills { get; set; }
            public int assists { get; set; }
        }

        public class Progression
        {
            public int level { get; set; }
            public int xp { get; set; }
        }

        public class Stats
        {
            public Ranked ranked { get; set; }
            public Casual casual { get; set; }
            public Overall overall { get; set; }
            public Progression progression { get; set; }
        }

        public class Player
        {
            public string username { get; set; }
            public string platform { get; set; }
            public string ubisoft_id { get; set; }
            public DateTime indexed_at { get; set; }
            public DateTime updated_at { get; set; }
            public Stats stats { get; set; }
        }

        public class RootObject
        {
            public Player player { get; set; }
        }
    }
}