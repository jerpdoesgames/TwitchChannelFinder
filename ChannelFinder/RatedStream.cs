namespace ChannelFinder
{
    public class RatedStream
    {
        public TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream streamData { get; set; }
        public float rating = 100;

        public RatedStream(TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream aStreamData)
        {
            streamData = aStreamData;
        }
    }
}
