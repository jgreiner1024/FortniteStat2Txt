using FortniteApi;
using FortniteApi.Data;
using FortniteApi.Response;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FortniteStat2Txt.Shared
{
    public class FortniteStatWriter
    {
        private const int NumberDaysToIncludeForRecentMatches = 1;
        private const int NumberOfSecondsBetweenUpdates = 10;

        private bool keepRunning = false;
        private string apiKey = null;
        private Platform platform = Platform.Pc;
        private string username = null;
        private string outputFolder = null;

        //private Dictionary<string, StreamWriter> filesStreams = new Dictionary<string, StreamWriter>();
        private Dictionary<string, string[]> modeStats = new Dictionary<string, string[]>();
        private Dictionary<string, string> friendlyNames = new Dictionary<string, string>();
        private string[] lifetimeStats;

        public event EventHandler<UpdatedEventArgs> Updated;


        public FortniteStatWriter(string apiKey, string epicUsername, string outputPath, Platform platform)
        {
            this.apiKey = apiKey;
            this.platform = platform;
            this.username = epicUsername;
            this.outputFolder = outputPath;

            InitializeStatDefinition();
            InitializeFriendlyNames();
        }

        private void InitializeStatDefinition()
        {
            //build out the mode stats we want
            modeStats.Add(Playlist.Solo, new string []
            {
                Stat.Top1,
                Stat.Top10,
                Stat.Top25
            });

            modeStats.Add(Playlist.Duo, new string[]
            {
                Stat.Top1,
                Stat.Top5,
                Stat.Top12
            });

            modeStats.Add(Playlist.Squad, new string[]
            {
                Stat.Top1,
                Stat.Top3,
                Stat.Top6
            });

            //build out the lifetime stats we want
            //we could add this to the mode stats and just add some extra logic in the write statement
            lifetimeStats = new string[]
            {
                "Matches Played",
                "Wins",
                "Win%",
                "Kills",
                "K/d"
            };
        }

        private void InitializeFriendlyNames()
        {
            //playlist friendly names
            friendlyNames.Add(Playlist.Solo, "Solo");
            friendlyNames.Add(Playlist.Duo, "Duo");
            friendlyNames.Add(Playlist.Squad, "Squad");

            //stat friendly names
            friendlyNames.Add(Stat.Top1, "Top1");
            friendlyNames.Add(Stat.Top3, "Top3");
            friendlyNames.Add(Stat.Top5, "Top5");
            friendlyNames.Add(Stat.Top6, "Top6");
            friendlyNames.Add(Stat.Top10, "Top10");
            friendlyNames.Add(Stat.Top12, "Top12");
            friendlyNames.Add(Stat.Top25, "Top25");
            friendlyNames.Add(Stat.AverageTimePlayed, "AverageTimePlayed");
            friendlyNames.Add(Stat.KillDeathRatio, "KillDeathRatio");
            friendlyNames.Add(Stat.Kills, "Kills");
            friendlyNames.Add(Stat.KillsPerMatch, "KillsPerMatch");
            friendlyNames.Add(Stat.Matches, "Matches");
            friendlyNames.Add(Stat.Score, "Score");
            friendlyNames.Add(Stat.ScorePerMatch, "ScorePerMatch");
            friendlyNames.Add(Stat.TrnRating, "TrnRating");
            friendlyNames.Add(Stat.WinRatio, "WinRatio");

            friendlyNames.Add("Matches Played", "MatchesPlayed");
            friendlyNames.Add("Wins", "Wins");
            friendlyNames.Add("Win%", "WinPercent");
            friendlyNames.Add("Kills", "Kills");
            friendlyNames.Add("K/d", "KillDeathRatio");
        }

        private string GetFriendlyName(string name)
        {
            return (friendlyNames.ContainsKey(name) == true) ? friendlyNames[name] : name;
        }

        //this is cache's the stream but it doesn't work since we have to open/close the file with each write
        //private StreamWriter GetStreamWriter(string key)
        //{
        //    if (filesStreams.ContainsKey(key) == false)
        //    {
        //        StreamWriter sw = new StreamWriter(new FileStream($"{outputFolder}{key}.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read));
        //        sw.AutoFlush = true;

        //        filesStreams.Add(key, sw);
        //    }

        //    //always reset the position back to the beginning
        //    filesStreams[key].BaseStream.Position = 0;
        //    return filesStreams[key];
        //}

        private void WriteStat(string filename, string value)
        {
            using (StreamWriter writer = new StreamWriter(new FileStream($"{outputFolder}{filename}.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read)))
            {
                writer.Write(value);
            }
        }
    


        private DateTime SafeParseDate(string date)
        {
            //go 1 day before the number of days to include variable so this stat isn't included in our recent matches
            DateTime invalidDate = DateTime.Now.AddDays((-NumberDaysToIncludeForRecentMatches) - 1);
            DateTime validDate;
            if (DateTime.TryParse(date, out validDate) == true)
                return validDate.ToLocalTime();

            return invalidDate;
        }

        public async Task Start()
        {
            if (outputFolder.EndsWith("\\") == false)
                outputFolder += "\\";

            //make sure the output folder exists, we don't have to check first though
            System.IO.Directory.CreateDirectory(outputFolder);

            using (FortniteClient client = new FortniteClient(this.apiKey))
            {
                //re-usable streamwriter reference

                //StreamWriter writer = null;

                keepRunning = true;
                while (keepRunning)
                {
                    ProfileResponse response = await client.FindPlayerAsync(platform, this.username);
                    
                    foreach (string modeName in modeStats.Keys)
                    {
                        foreach (string statName in modeStats[modeName])
                        {
                            WriteStat(
                                $"{GetFriendlyName(modeName)}_{GetFriendlyName(statName)}", //filename
                                $"{response.Stats[modeName][statName].Value}" //value
                            );
                        }
                    }

                    foreach(string statName in lifetimeStats)
                    {
                        WriteStat(
                            $"Lifetime_{GetFriendlyName(statName)}", //filename
                            $"{response.LifeTimeStats.FirstOrDefault(lifetimeStat => lifetimeStat.Key == statName)?.Value}" //value
                        );
                    }

                    DateTime yesterday = DateTime.Now.AddDays(-NumberDaysToIncludeForRecentMatches);
                    var matches = response.RecentMatches.Where(match => SafeParseDate(match.DateCollected) >= yesterday);

                    WriteStat(
                        $"RecentMatches_Kills", //filename
                        $"{matches.Sum(match => match.Kills)}" //value
                    );

                    WriteStat(
                        $"RecentMatches_Matches", //filename
                        $"{matches.Sum(match => match.Matches)}" //value
                    );

                    WriteStat(
                        $"RecentMatches_TotalWins", //filename
                        $"{matches.Sum(match => match.Top1)}" //value
                    );

                    //send an updated event
                    Updated?.Invoke(this, new UpdatedEventArgs());

                    await Task.Delay(1000 * NumberOfSecondsBetweenUpdates);
                }
            }

            //close all the filestreams
            //foreach (string key in filesStreams.Keys)
            //{
            //    filesStreams[key].Close();
            //}
        }

        public void Stop()
        {
            keepRunning = false;
        }
    }
}
