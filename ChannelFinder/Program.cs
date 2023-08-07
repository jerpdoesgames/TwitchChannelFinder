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

                        // Get other games to add to the list (currently just whatever the streamer was last playing)
                        List<string> getGameList = new List<string>();
                        getGameList.Add(baseChannelInfoTask.Result.Data[0].GameName);
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
                                Console.WriteLine(string.Join("|", new string[] { curStream.rating.ToString(), curStream.userName, curStream.game, curStream.title, string.Join(", ",curStream.tags), (Math.Round(Criteria.getTimeSinceStart(curStream).TotalMinutes, 0).ToString() + "m"), (curStream.viewerCount + " Viewers"), curStream.followerOnly ? "Follower-Only" : null }));
                            }
                        }
                    }
                }
            }
        }
    }
}
