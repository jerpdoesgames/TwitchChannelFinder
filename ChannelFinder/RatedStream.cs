using System;
using System.Threading.Tasks;
using TwitchLib.Api;

namespace ChannelFinder
{
    public class RatedStream
    {
        public float rating = 50;

        public string title { get; set; }
        public string[] tags { get; set; }
        public string language { get; set; }
        public string game { get; set; }
        public int viewerCount { get; set; }
        public System.DateTime startedAt { get; set; }
        public string broadcasterID { get; set; }
        public bool followerOnly { get; set; }
        public string userName { get; set; }
        private string m_Thumbnail;
        public string thumbnail {
            get { return m_Thumbnail; }
            set
            {
                m_Thumbnail = value.Replace("{width}", 320.ToString());
                m_Thumbnail = m_Thumbnail.Replace("{height}", 180.ToString());
            }
        }


        private void updateFollowerOnly(TwitchAPI aAPIObject)
        {

            Task<TwitchLib.Api.Helix.Models.Chat.ChatSettings.GetChatSettingsResponse> chatSettingsTask = Task.Run(() => aAPIObject.Helix.Chat.GetChatSettingsAsync(broadcasterID, broadcasterID));
            chatSettingsTask.Wait();

            if (chatSettingsTask.Result != null && chatSettingsTask.Result.Data.Length >= 0)
            {
                TwitchLib.Api.Helix.Models.Chat.ChatSettings.GetChatSettingsResponse curResponse = chatSettingsTask.Result;
                followerOnly = curResponse.Data[0].FollowerMode;
            }

            System.Threading.Thread.Sleep(100);
        }

        public RatedStream(TwitchAPI aAPIObject, TwitchLib.Api.Helix.Models.Streams.GetFollowedStreams.Stream aStreamData, float aInitialRating = 50)
        {
            rating = aInitialRating;

            title = aStreamData.Title;
            tags = aStreamData.Tags;
            language = aStreamData.Language;
            game = aStreamData.GameName;
            viewerCount = aStreamData.ViewerCount;
            startedAt = aStreamData.StartedAt;
            broadcasterID = aStreamData.UserId;
            userName = aStreamData.UserName;
            thumbnail = aStreamData.ThumbnailUrl;

            updateFollowerOnly(aAPIObject);
        }

        public RatedStream(TwitchAPI aAPIObject, TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream aStreamData, float aInitialRating = 50)
        {
            rating = aInitialRating;

            title = aStreamData.Title;
            tags = aStreamData.Tags;
            language = aStreamData.Language;
            game = aStreamData.GameName;
            viewerCount = aStreamData.ViewerCount;
            startedAt = aStreamData.StartedAt;
            broadcasterID = aStreamData.UserId;
            userName = aStreamData.UserName;
            thumbnail = aStreamData.ThumbnailUrl;

            updateFollowerOnly(aAPIObject);
        }
    }
}
