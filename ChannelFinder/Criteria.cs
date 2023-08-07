using System;
using System.Collections.Generic;

namespace ChannelFinder
{
    public class CriteriaElement
    {
        public string language { get; set; }
        public string game { get; set; }
        public string channel { get; set; }
        public string action { get; set; }
        public string tag { get; set; }
        public float value { get; set; }
        public int priority { get; set; }
        public int viewersMin { get; set; }
        public int viewersMax { get; set; }
        public int minutesLiveMin { get; set; }
        public int minutesLiveMax { get; set; }
        public int followerOnly { get; set; }

        public CriteriaElement()
        {
            viewersMin = -1;
            viewersMax = -1;
            minutesLiveMin = -1;
            minutesLiveMax = -1;
            followerOnly = -1;  // -1 for no check, 0 for no, 1 for yes
        }
    }


    public class Criteria
    {
        public static TimeSpan getTimeSinceStart(RatedStream aStream)
        {
            return DateTime.Now.ToUniversalTime().Subtract(aStream.streamData.StartedAt);
        }
        private bool hasTag(string tagName, RatedStream curStream)
        {
            for (int i=0; i < curStream.tags.Length; i++)
            {
                string curTag = curStream.tags[i];

                if (tagName == curTag.ToLower())
                    return true;
            }

            return false;
        }

        private bool isMatch(CriteriaElement curCriteria, RatedStream curStream)
        {
            string checkLang = curCriteria.language;
            string checkGame = curCriteria.game;
            int viewerCount = curStream.streamData.ViewerCount;

            if (checkLang == "[[SAME]]")
                checkLang = baseChannel.BroadcasterLanguage;

            if (checkGame == "[[SAME]]")
                checkGame = baseChannel.GameName;

            if (curCriteria.viewersMin != -1 && viewerCount < curCriteria.viewersMin)
                return false;

            if (curCriteria.viewersMax != -1 && viewerCount > curCriteria.viewersMax)
                return false;

            if (!string.IsNullOrEmpty(checkLang) && checkLang != curStream.streamData.Language)
                return false;

            if (!string.IsNullOrEmpty(checkGame) && curStream.streamData.GameName.ToLower().IndexOf(checkGame.ToLower()) == -1)
                return false;

            if (!string.IsNullOrEmpty(curCriteria.channel) && curCriteria.channel.ToLower() != curStream.streamData.UserName.ToLower())
                return false;


            if (!string.IsNullOrEmpty(curCriteria.tag) && !hasTag(curCriteria.tag.ToLower(), curStream))
                return false;

            double liveSeconds = getTimeSinceStart(curStream).TotalSeconds;

            if (curCriteria.minutesLiveMin != -1 && liveSeconds < (curCriteria.minutesLiveMin * 60) )
                return false;

            if (curCriteria.minutesLiveMax != -1 && liveSeconds > (curCriteria.minutesLiveMax * 60))
                return false;

            return true;
        }

        private void modifyScore(CriteriaElement curCriteria, ref RatedStream curStream)
        {
            switch (curCriteria.action)
            {
                case "add":
                    curStream.rating += curCriteria.value;
                    break;
                case "subtract":
                    curStream.rating -= curCriteria.value;
                    break;
                case "multiply":
                    curStream.rating *= curCriteria.value;
                    break;
                case "divide":
                    curStream.rating /= curCriteria.value;
                    break;
                default:
                    Console.WriteLine("Found invalid action type for criteria |" + curCriteria.game + "|" + curCriteria.channel);
                    break;
            }
        }

        public List<CriteriaElement> entries { get; set; }
        public TwitchLib.Api.Helix.Models.Channels.GetChannelInformation.ChannelInformation baseChannel { get; set; }

        public void calculateRating(ref RatedStream curStream)
        {
            foreach (CriteriaElement curCriteria in entries)
            {
                if (isMatch(curCriteria, curStream))
                {
                    modifyScore(curCriteria, ref curStream);
                }
            }
        }
    }
}
