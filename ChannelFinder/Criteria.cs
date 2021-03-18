using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChannelFinder
{
    public class CriteriaElement
    {
        public string game { get; set; }
        public string channel { get; set; }
        public string action { get; set; }
        public string tag { get; set; }
        public float value { get; set; }
        public int priority { get; set; }
    }


    public class Criteria
    {
        private bool hasTag(string tagName, RatedStream curStream)
        {
            for (int i=0; i < curStream.tagData.Length; i++)
            {
                TwitchLib.Api.Helix.Models.Common.Tag curTag = curStream.tagData[i];

                // Rather than require a language to be specified, just check every language for a match.  Probably not a performance concern since this is running locally and Twitch is giving you every language by default.
                foreach (KeyValuePair<string, string> curLocale in curStream.tagData[i].LocalizationNames)
                {
                    if (tagName == curLocale.Value.ToLower())
                        return true;
                }
            }

            return false;
        }

        private bool isMatch(CriteriaElement curCriteria, RatedStream curStream)
        {
            string checkGame = curCriteria.game;

            if (checkGame == "[[SAME]]")
                checkGame = baseChannel.Game;

            if (!string.IsNullOrEmpty(checkGame) && checkGame != curStream.streamData.GameName)
                return false;

            if (!string.IsNullOrEmpty(curCriteria.channel) && curCriteria.channel.ToLower() != curStream.streamData.UserName.ToLower())
                return false;

            if (!string.IsNullOrEmpty(curCriteria.tag) && !hasTag(curCriteria.tag.ToLower(), curStream))
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
        public TwitchLib.Api.V5.Models.Channels.Channel baseChannel { get; set; }

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
