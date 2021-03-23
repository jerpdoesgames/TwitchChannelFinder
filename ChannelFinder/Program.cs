using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using TwitchLib.Api;
using System.Collections.Generic;

namespace ChannelFinder
{
    class Program
    {
        private static string storagePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ChannelFinder");
        private const int FOLLOWING_PER_PAGE = 100;
        private const int MAX_NONFOLLOWED_PER_PAGE = 5;
        private const int PAGE_COUNT_MAX = 10;  // Safety in case grabbing more than 1000 channels (and... I suppose up to 1000 streams) is too much to get in short order

        public static List<string> getStreamList(List<TwitchLib.Api.V5.Models.Users.UserFollow> followingList, int start = 0, int count = 25)
        {
            List<string> outputList = new List<string>();

            for (int i = start; i < count + start && i < followingList.Count; i++)
            {
                outputList.Add(followingList[i].Channel.Id);
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

                    Task<TwitchLib.Api.V5.Models.Channels.Channel> baseChannelInfoTask = Task.Run(() => apiObject.V5.Channels.GetChannelByIDAsync(finderConfig.channel_id.ToString()));
                    baseChannelInfoTask.Wait();

                    Task<TwitchLib.Api.V5.Models.Users.Users> baseUserInfoTask = Task.Run(() => apiObject.V5.Users.GetUserByNameAsync(baseChannelInfoTask.Result.Name));
                    baseUserInfoTask.Wait();

                    if (baseUserInfoTask.Result.Total == 1)
                    {
                        finderCriteria.baseChannel = baseChannelInfoTask.Result;
                        List<TwitchLib.Api.V5.Models.Users.UserFollow> followerList = new List<TwitchLib.Api.V5.Models.Users.UserFollow>();

                        Task<TwitchLib.Api.V5.Models.Users.UserFollows> userFollowsTask = Task.Run(() => apiObject.V5.Users.GetUserFollowsAsync(baseUserInfoTask.Result.Matches[0].Id, FOLLOWING_PER_PAGE, 0));
                        userFollowsTask.Wait();

                        followerList.AddRange(userFollowsTask.Result.Follows);

                        // Get other games to add to the list (currently just whatever the streamer was last playing)
                        List<string> getGameList = new List<string>();
                        getGameList.Add(baseChannelInfoTask.Result.Game);
                        Task<TwitchLib.Api.Helix.Models.Games.GetGamesResponse> getGameIDTask = Task.Run(() => apiObject.Helix.Games.GetGamesAsync(null, getGameList));
                        getGameIDTask.Wait();

                        List<string> gameIDList = new List<string>();
                        for (int i=0; i < getGameIDTask.Result.Games.Length; i++)
                        {
                            gameIDList.Add(getGameIDTask.Result.Games[i].Id);
                        }

                        // Get a few streams (regardless of follow status) from those other games
                        Task<TwitchLib.Api.Helix.Models.Streams.GetStreams.GetStreamsResponse> getGameStreamsTask = Task.Run(() => apiObject.Helix.Streams.GetStreamsAsync(null, null, MAX_NONFOLLOWED_PER_PAGE, gameIDList));
                        getGameStreamsTask.Wait();

                        List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream> streamList = new List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream>(getGameStreamsTask.Result.Streams);
                        List<RatedStream> ratedStreams = new List<RatedStream>();
                        RatedStream curRatedStream;

                        if (followerList.Count > 0)
                        {
                            List<string> queryIDList = getStreamList(followerList, 0, FOLLOWING_PER_PAGE);
                            Task<TwitchLib.Api.Helix.Models.Streams.GetStreams.GetStreamsResponse> getStreamsTask = Task.Run(() => apiObject.Helix.Streams.GetStreamsAsync(null, null, FOLLOWING_PER_PAGE, null, null, "all", queryIDList));
                            getStreamsTask.Wait();

                            // Probably rearrange this a bit so I can addrange earlier for multiple games worth of possibly-not-followed 
                            streamList = new List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream>(getStreamsTask.Result.Streams);
                            streamList.AddRange(getGameStreamsTask.Result.Streams);

                            for (int i = 0; i < streamList.Count; i++)
                            {
                                curRatedStream = new RatedStream(apiObject, streamList[i]);
                                finderCriteria.calculateRating(ref curRatedStream);
                                ratedStreams.Add(curRatedStream);
                            }

                            ratedStreams.Sort((a, b) => b.rating.CompareTo(a.rating));

                            foreach(RatedStream curStream in ratedStreams)
                            {
                                Console.WriteLine(string.Join("|", new string[] { curStream.rating.ToString(), curStream.streamData.UserName, curStream.streamData.GameName, curStream.streamData.Title, string.Join(", ",curStream.tagList(baseChannelInfoTask.Result.Language)) }));
                            }

                            Console.WriteLine("got some data");
                        }
                    }
                }
            }
        }
    }
}
