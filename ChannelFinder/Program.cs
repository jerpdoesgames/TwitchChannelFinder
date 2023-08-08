using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TwitchLib.Api;

// https://id.twitch.tv/oauth2/authorize?client_id=[client_id]&redirect_uri=http://localhost&response_type=token&scope=user:read:follows
// Required scopes: user:read:follows

namespace ChannelFinder
{
    class Program
    {
        private static string storagePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ChannelFinder");
        private const int FOLLOWING_PER_PAGE = 100;
        private const int MAX_NONFOLLOWED_PER_PAGE = 20;
        private const int PAGE_COUNT_MAX = 10;  // Safety in case grabbing more than 1000 channels (and... I suppose up to 1000 streams) is too much to get in short order

        public static List<string> getStreamList(List<TwitchLib.Api.Helix.Models.Users.GetUserFollows.Follow> followingList, int start = 0, int count = 25)
        {
            List<string> outputList = new List<string>();

            for (int i = start; i < count + start && i < followingList.Count; i++)
            {
                outputList.Add(followingList[i].ToUserId);
            }

            return outputList;
        }

        static void Main(string[] args)
        {
            Config finderConfig;
            Criteria finderCriteria;

            string criteriaPath = System.IO.Path.Combine(storagePath, "channel_criteria.json");
            string configPath = System.IO.Path.Combine(storagePath, "finder_config.json");

            string outputPath = System.IO.Path.Combine(storagePath, "output","output.html");

            string pageTemplate = @"
                <!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
                <html xmlns=""http://www.w3.org/1999/xhtml"">
                <head>
                    <title>Twitch Channel Finder - By Jerp</title>
                    <meta charset=""UTF-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                    <link rel=""stylesheet"" type=""text/css"" href=""style.css"" />
                </head>

                <body>
                {0}
                </body>

                </html>
            ";

            string streamTemplate = @"
                <h2 class=""streamChannel""><span class=""streamScore"">{6}</span><a href=""https://twitch.tv/{0}"">{0}</a></h2>
                <h3 class=""streamTitle"">{1}</a></h3>
                <p>{7}</p>
                <a href=""https://twitch.tv/{0}""><img class=""streamThumbnail"" src=""{2}""/></a>
                <div class=""streamTagList"">
                    {3}
                </div>
                {8}
                <div class=""streamMiscInfo"">
                    {4} viewers<br/>
                    Streaming for {5} minutes<br/>
                </div>
                <hr/>
            ";

            string tagTemplate = @"
                <span class=""streamTag"">{0}</span>
            ";

            if (File.Exists(configPath))
            {
                string configText = File.ReadAllText(configPath);
                finderConfig = JsonConvert.DeserializeObject<Config>(configText);

                if (File.Exists(criteriaPath))
                {
                    string criteriaText = File.ReadAllText(criteriaPath);
                    finderCriteria = JsonConvert.DeserializeObject<Criteria>(criteriaText);

                    TwitchAPI apiObject = new TwitchAPI();
                    apiObject.Settings.AccessToken = finderConfig.oauth;
                    apiObject.Settings.ClientId = finderConfig.client_id;

                    Task<TwitchLib.Api.Helix.Models.Channels.GetChannelInformation.GetChannelInformationResponse> baseChannelInfoTask = Task.Run(() => apiObject.Helix.Channels.GetChannelInformationAsync(finderConfig.channel_id.ToString()));
                    baseChannelInfoTask.Wait();

                    Task<TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse> baseUserInfoTask = Task.Run(() => apiObject.Helix.Users.GetUsersAsync(new List<string> { baseChannelInfoTask.Result.Data[0].BroadcasterId }));
                    baseUserInfoTask.Wait();

                    if (baseUserInfoTask.Result.Users.Length == 1)
                    {
                        finderCriteria.baseChannel = baseChannelInfoTask.Result.Data[0];
                        List<TwitchLib.Api.Helix.Models.Streams.GetFollowedStreams.Stream> followedStreamsList = new List<TwitchLib.Api.Helix.Models.Streams.GetFollowedStreams.Stream>();

                        int curPage = 1;
                        string curCursor = null;
                        while (curPage <= PAGE_COUNT_MAX)
                        {
                            Task<TwitchLib.Api.Helix.Models.Streams.GetFollowedStreams.GetFollowedStreamsResponse> followedStreamsTask = Task.Run(() => apiObject.Helix.Streams.GetFollowedStreamsAsync(baseUserInfoTask.Result.Users[0].Id, FOLLOWING_PER_PAGE, curCursor));

                            followedStreamsTask.Wait();

                            followedStreamsList.AddRange(followedStreamsTask.Result.Data);
                            curCursor = followedStreamsTask.Result.Pagination.Cursor;

                            if (followedStreamsTask.Result.Data.Length == 0 || string.IsNullOrEmpty(curCursor))
                            {
                                break;
                            }
                            System.Threading.Thread.Sleep(250);
                            curPage++;
                        }

                        // Get other games to add to the list
                        List<string> getGameList = new List<string>();
                        getGameList.Add(baseChannelInfoTask.Result.Data[0].GameName);
                        if (finderCriteria.categories != null && finderCriteria.categories.Count > 0)
                        {
                            foreach (string curCategory in finderCriteria.categories)
                            {
                                if (getGameList.Count < 100 && curCategory.ToLower() != baseChannelInfoTask.Result.Data[0].GameName.ToLower())  // TODO: make 100 a global -- it's the total categories you can request at a time
                                {
                                    getGameList.Add(curCategory);
                                }
                            }
                        }
                        
                        Task<TwitchLib.Api.Helix.Models.Games.GetGamesResponse> getGameIDTask = Task.Run(() => apiObject.Helix.Games.GetGamesAsync(null, getGameList));
                        getGameIDTask.Wait();

                        List<string> gameIDList = new List<string>();
                        for (int i=0; i < getGameIDTask.Result.Games.Length; i++)
                        {
                            gameIDList.Add(getGameIDTask.Result.Games[i].Id);
                        }

                        // Get a few streams (regardless of follow status) from those other games
                        Task<TwitchLib.Api.Helix.Models.Streams.GetStreams.GetStreamsResponse> getGameStreamsTask = Task.Run(() => apiObject.Helix.Streams.GetStreamsAsync(null, MAX_NONFOLLOWED_PER_PAGE, gameIDList));
                        getGameStreamsTask.Wait();

                        List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream> notFollowedStreamList = new List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream>(getGameStreamsTask.Result.Streams);
                        List<RatedStream> ratedStreams = new List<RatedStream>();
                        RatedStream curRatedStream;


                        string streamOutputPageContents = "";

                        if (followedStreamsList.Count > 0 || notFollowedStreamList.Count > 0)
                        {
                            foreach (TwitchLib.Api.Helix.Models.Streams.GetFollowedStreams.Stream curStream in followedStreamsList)
                            {
                                curRatedStream = new RatedStream(apiObject, curStream);
                                finderCriteria.calculateRating(ref curRatedStream);
                                ratedStreams.Add(curRatedStream);
                            }

                            foreach (TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream curStream in notFollowedStreamList)
                            {
                                curRatedStream = new RatedStream(apiObject, curStream);
                                finderCriteria.calculateRating(ref curRatedStream);
                                ratedStreams.Add(curRatedStream);
                            }

                            ratedStreams.Sort((a, b) => b.rating.CompareTo(a.rating));

                            foreach(RatedStream curStream in ratedStreams)
                            {
                                Console.WriteLine(string.Join("|", new string[] { curStream.rating.ToString(), curStream.userName, curStream.game, curStream.title, curStream.tags != null ? string.Join(", ", curStream.tags) : "", (Math.Round(Criteria.getTimeSinceStart(curStream).TotalMinutes, 0).ToString() + "m"), (curStream.viewerCount + " Viewers"), curStream.followerOnly ? "Follower-Only" : "" }));
                                string streamOutputPageTags = "";

                                if (curStream.tags != null && curStream.tags.Length > 0)
                                {
                                    foreach (string curTag in curStream.tags)
                                    {
                                        streamOutputPageTags += string.Format(tagTemplate, curTag);
                                    }
                                }

                                string followerOnlyWarning = curStream.followerOnly ? "<p>FOLLOWER-ONLY</p>" : "";
                                streamOutputPageContents += string.Format(streamTemplate, curStream.userName, curStream.title, curStream.thumbnail, streamOutputPageTags, curStream.viewerCount.ToString(), (Math.Round(Criteria.getTimeSinceStart(curStream).TotalMinutes, 0).ToString()), Math.Round(curStream.rating, 0).ToString(), curStream.game, followerOnlyWarning);
                            }
                        }

                        File.WriteAllText(outputPath, string.Format(pageTemplate, streamOutputPageContents));
                        System.Diagnostics.Process.Start(outputPath);
                    }
                }
            }
        }
    }
}
