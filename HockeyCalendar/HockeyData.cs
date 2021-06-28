using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace HockeyCalendar
{
    public class HockeyData
    {
        private const string NhlApi = "https://statsapi.web.nhl.com/api/v1/";
        public class TeamInfo : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public TeamInfo()
            {
                _selected = false;
                NeedsUpdate = true;
                Games = new HashSet<long>();
            }

            public int Id { get; set; }

            public string Name { get; set; }

            public string Abbrev { get; set; }

            private bool _selected;
            public bool Selected
            {
                get { return _selected; }

                set
                {
                    if (_selected != value)
                    {
                        _selected = value;

                        if (PropertyChanged != null)
                        {
                            PropertyChanged(this, new PropertyChangedEventArgs("Selected"));
                        }
                    }
                }
            }

            public HashSet<long> Games { get; set; }
            internal bool NeedsUpdate { set; get; }
        }

        public static Dictionary<int, int> TeamIdToTeamInfoIndex = new Dictionary<int, int>();

        public static ObservableCollection<TeamInfo> AllTeams = new ObservableCollection<TeamInfo>();

        public static string GetTeamNameFromId(int id)
        {
            if (TeamIdToTeamInfoIndex.ContainsKey(id))
            {
                return AllTeams[TeamIdToTeamInfoIndex[id]].Name;
            }
            else
            {
                return "";
            }
        }

        public struct ScheduledGameInfo
        {
            public DateTime GameDateTime { get; set; }
            public string DisplayedDateTime { get; set; }
            public string HomeTeam { get; set; }
            public string AwayTeam { get; set; }
        }
        static Dictionary<long, ScheduledGameInfo> ScheduledGameIdToGameInfo = new Dictionary<long, ScheduledGameInfo>();
        public static ObservableCollection<ScheduledGameInfo> GamesSchedule = new ObservableCollection<ScheduledGameInfo>();

        internal static void UpdateTeamNames()
        {
            string teamInfoResponseJson = null;
            try
            {
                teamInfoResponseJson = RequestAllTeamsInfo();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to get server response for team names, error message was: {ex.Message}");
                teamInfoResponseJson = null;
            }

            if (!String.IsNullOrEmpty(teamInfoResponseJson))
            {
                try
                {
                    ParseTeamInfoJsonReponse(teamInfoResponseJson);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to parse server response for team names, error message was: {ex.Message}");
                }
            }
        }

        private static string RequestAllTeamsInfo()
        {
            string responseJson = null;
            Uri requestUri = new Uri(NhlApi + "teams");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                responseJson = reader.ReadToEnd();
            }

            return responseJson;
        }

        private static void ParseTeamInfoJsonReponse(string rsp)
        {
            using (JsonDocument document = JsonDocument.Parse(rsp))
            {
                var teamsEnum = document.RootElement.GetProperty("teams").EnumerateArray();
                while (teamsEnum.MoveNext())
                {
                    int id = teamsEnum.Current.GetProperty("id").GetInt16();
                    string name = teamsEnum.Current.GetProperty("name").GetString();
                    string abbreviation = teamsEnum.Current.GetProperty("abbreviation").GetString();

                    if (!TeamIdToTeamInfoIndex.ContainsKey(id))
                    {
                        TeamIdToTeamInfoIndex.Add(id, AllTeams.Count);
                        AllTeams.Add(new TeamInfo { Id = id, Name = name, Abbrev = abbreviation });
                    }

                    Console.WriteLine($"Parsed info for {name}.");
                }
            }
        }

        internal static void UpdateGameScheduleForTeam(int teamId)
        {
            string scheduleResponseJson = null;
            try
            {
                scheduleResponseJson = RequestGameScheduleJsonResponse(teamId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to get server response for games schedule, error message was: {ex.Message}");
                scheduleResponseJson = null;
            }

            if (!String.IsNullOrEmpty(scheduleResponseJson))
            {
                try
                {
                    ParseGameScheduleJsonReponse(scheduleResponseJson, teamId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to parse server response for games schedule, error message was: {ex.Message}");
                }
            }
        }

        private static string RequestGameScheduleJsonResponse(int teamId)
        {
            string responseJson = null;

            DateTime todayDate = DateTime.Today;
            string todayDateStr = todayDate.ToString("yyyy-MM-dd");
            DateTime toEndWeek = todayDate.AddDays(6);
            string toEndWeekStr = toEndWeek.ToString("yyyy-MM-dd");

            string requestUriStr = NhlApi + "schedule?" + "teamId=" + teamId + "&startDate=" + todayDateStr + "&endDate=" + toEndWeekStr;
            Uri requestUri = new Uri(requestUriStr);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                responseJson = reader.ReadToEnd();
            }

            return responseJson;
        }

        private static void ParseGameScheduleJsonReponse(string rsp, int teamId)
        {
            using (JsonDocument document = JsonDocument.Parse(rsp))
            {
                int totalGames = document.RootElement.GetProperty("totalGames").GetInt16();

                if (totalGames > 0)
                {
                    var datesEnum = document.RootElement.GetProperty("dates").EnumerateArray();
                    while (datesEnum.MoveNext())
                    {
                        string currentDate = datesEnum.Current.GetProperty("date").GetString();
                        var parsedDate = DateTime.Parse(currentDate);

                        // seems the nhl api is giving the wrong day for games at midnight utc?
                        // or I've just botched the localtime conversion somehow
                        if (parsedDate.Hour == 0)
                        {
                            Console.WriteLine("Corrected UTC Midnight");
                            parsedDate = parsedDate.AddDays(1);
                        }

                        var localDateTime = parsedDate.ToLocalTime();
                        Console.WriteLine("Converted {0} to {1}.", parsedDate, localDateTime);

                        TimeZoneInfo localZone = TimeZoneInfo.Local;
                        string displayedDateTime = localDateTime.ToString("dddd, MMMM dd, yyyy, h:mm" + $" {localZone.StandardName}");

                        int totalGamesOnDate = datesEnum.Current.GetProperty("totalGames").GetInt16();

                        if (totalGamesOnDate > 0)
                        {
                            var gamesEnum = datesEnum.Current.GetProperty("games").EnumerateArray();
                            while (gamesEnum.MoveNext())
                            {
                                long gameId = gamesEnum.Current.GetProperty("gamePk").GetInt64();
                                var teamsElement = gamesEnum.Current.GetProperty("teams");
                                int homeId = teamsElement.GetProperty("home").GetProperty("team").GetProperty("id").GetInt16();
                                int awayId = teamsElement.GetProperty("away").GetProperty("team").GetProperty("id").GetInt16();

                                if (!ScheduledGameIdToGameInfo.ContainsKey(gameId))
                                {
                                    ScheduledGameIdToGameInfo.Add(gameId, new ScheduledGameInfo
                                    {
                                        DisplayedDateTime = displayedDateTime,
                                        GameDateTime = localDateTime,
                                        HomeTeam = AllTeams[TeamIdToTeamInfoIndex[homeId]].Name,
                                        AwayTeam = AllTeams[TeamIdToTeamInfoIndex[awayId]].Name
                                    });
                                }

                                if (TeamIdToTeamInfoIndex.ContainsKey(teamId))
                                {
                                    AllTeams[TeamIdToTeamInfoIndex[teamId]].Games.Add(gameId);
                                }

                                Console.WriteLine("Home: {0}", GetTeamNameFromId(homeId));
                                Console.WriteLine("Away: {0}", GetTeamNameFromId(awayId));
                            }
                        }
                    }
                }

                AllTeams[TeamIdToTeamInfoIndex[teamId]].NeedsUpdate = false;
            }
        }

        public static void UpdateGameSchedule()
        {
            GamesSchedule.Clear();
            List<long> games = new List<long>();

            foreach (TeamInfo teamInfo in AllTeams)
            {
                if (teamInfo.Selected)
                {
                    if (teamInfo.NeedsUpdate)
                    {
                        UpdateGameScheduleForTeam(teamInfo.Id);
                    }

                    foreach (long gameId in teamInfo.Games)
                    {
                        games.Add(gameId);
                    }
                }
            }

            games = games.Distinct().ToList();
            games.Sort();
            foreach (long gameId in games)
            {
                GamesSchedule.Add(ScheduledGameIdToGameInfo[gameId]);
            }
        }
    }
}
