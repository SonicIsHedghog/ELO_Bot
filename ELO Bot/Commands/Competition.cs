using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace ELO_Bot.Commands
{
    public class Competition : ModuleBase
    {
        //Whats Needed:
        //Create Comp !
        //Delete Comp
        //Create Team !
        //Delete Team
        //Join Team .
        //Leave Team
        //Method for deciding the competition structure
        //Team Win / Team Lose
        //Object and data that matches the teams
        //competition announcements
        //


        [Command("Create Competition")]
        [Summary("CreateCompetition <teams> <players per team> <description>")]
        [Remarks("Create a knockout competition for the current lobby")]
        public async Task CreateComp(int teamcount, int teamusers, [Remainder] string description)
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);

            if (server.ServerCompetitions.Any(x => x.Channel == Context.Channel.Id))
            {
                return;
            }

            var comp = new Servers.Server.Competition
            {
                Channel = Context.Channel.Id,
                Info = description,
                TeamLimit = teamcount,
                PlayerLimit = teamusers,
                Teams = new List<Servers.Server.Competition.Team>()
            };

            server.ServerCompetitions.Add(comp);
            await ReplyAsync("Done Boi");
        }

        [Command("Create Team")]
        [Summary("Create Team <Teamname>")]
        [Remarks("Create a team for the current competition")]
        public async Task CreateTeam(string teamName)
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            var competition = server.ServerCompetitions.FirstOrDefault(x => x.Channel == Context.Channel.Id);
            if (competition == null)
            {
                //no competition
                await ReplyAsync("No Competition here.");
                return;
            }

            if (competition.Teams.Count < competition.TeamLimit && competition.Teams.All(x => x.Teamname != teamName))
            {
                var comp = new Servers.Server.Competition.Team
                {
                    Teamname = teamName,
                    TeamCaptain = Context.User.Id,
                    Players = new List<ulong>
                    { Context.User.Id }
                };


                competition.Teams.Add(comp);
            }
        }

        [Command("Join Team")]
        [Summary("Join Team <TeamName>")]
        [Remarks("Join a team")]
        public async Task JoinTeam(string teamName)
        {
            var server = Servers.ServerList.First(x => x.ServerId == Context.Guild.Id);
            var competition = server.ServerCompetitions.FirstOrDefault(x => x.Channel == Context.Channel.Id);
            if (competition == null)
            {
                //no competition
                await ReplyAsync("No Competition here.");
                return;
            }

            if (competition.Teams.Any(x => x.Players.Contains(Context.User.Id)))
            {
                await ReplyAsync("Already in a team.");
                return;
            }
        }
    }
}
