using System.Threading.Tasks;
using TwitchLib.Api;
using System.Collections.Generic;

namespace ChannelFinder
{
    public class RatedStream
    {
        public TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream streamData { get; set; }
        public TwitchLib.Api.Helix.Models.Common.Tag[] tagData { get; set; }
        public float rating = 100;

        public List<string> tagList(string aBaseLanguage)
        {
            List<string> output = new List<string>();

            // TODO: Switch to just plain string tags
            foreach (TwitchLib.Api.Helix.Models.Common.Tag curTag in tagData)
            {
                foreach (KeyValuePair<string, string> curLocale in curTag.LocalizationNames)
                {
                    if (curLocale.Key.IndexOf(aBaseLanguage) == 0)    // Starts with the code for the base language ('en' for 'en-us')
                        output.Add(curLocale.Value);
                }
            }

            return output;
        }

        public RatedStream(TwitchAPI aAPIObject, TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream aStreamData, float aInitialRating = 100)
        {
            streamData = aStreamData;
            rating = aInitialRating;


            // TODO: replace with get channel information:
            // https://dev.twitch.tv/docs/api/reference/#get-stream-tags
            // https://dev.twitch.tv/docs/api/reference/#get-channel-information
            Task<TwitchLib.Api.Helix.Models.Streams.GetStreamTags.GetStreamTagsResponse> getStreamTagsTask = Task.Run(() => aAPIObject.Helix.Streams.GetStreamTagsAsync(streamData.UserId.ToString()));
            getStreamTagsTask.Wait();

            tagData = getStreamTagsTask.Result.Data;
        }
    }
}
