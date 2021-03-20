using System.Threading.Tasks;
using TwitchLib.Api;

namespace ChannelFinder
{
    public class RatedStream
    {
        public TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream streamData { get; set; }
        public TwitchLib.Api.Helix.Models.Common.Tag[] tagData { get; set; }
        public float rating = 100;

        public RatedStream(TwitchAPI aAPIObject, TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream aStreamData, float aInitialRating = 100)
        {
            streamData = aStreamData;
            rating = aInitialRating;

            Task<TwitchLib.Api.Helix.Models.Streams.GetStreamTags.GetStreamTagsResponse> getStreamTagsTask = Task.Run(() => aAPIObject.Helix.Streams.GetStreamTagsAsync(streamData.UserId.ToString()));
            getStreamTagsTask.Wait();

            tagData = getStreamTagsTask.Result.Data;
        }
    }
}
