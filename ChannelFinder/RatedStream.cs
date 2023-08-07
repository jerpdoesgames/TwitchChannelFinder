using System.Threading.Tasks;
using TwitchLib.Api;
using System.Collections.Generic;

namespace ChannelFinder
{
    public class RatedStream
    {
        public float rating = 100;

        string title { get; set; }
        string[] tags { get; set; }


        public RatedStream(TwitchAPI aAPIObject, TwitchLib.Api.Helix.Models.Streams.GetFollowedStreams.Stream aStreamData, float aInitialRating = 100)
        {

            title = aStreamData.Title;
            tags = aStreamData.Tags;

            rating = aInitialRating;


            // TODO: replace with get channel information:
            // https://dev.twitch.tv/docs/api/reference/#get-stream-tags
            // https://dev.twitch.tv/docs/api/reference/#get-channel-information
        }

        public RatedStream(TwitchAPI aAPIObject, TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream aStreamData, float aInitialRating = 100)
        {
            rating = aInitialRating;


            // TODO: replace with get channel information:
            // https://dev.twitch.tv/docs/api/reference/#get-stream-tags
            // https://dev.twitch.tv/docs/api/reference/#get-channel-information
        }
    }
}
