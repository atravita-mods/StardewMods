using StardewModdingAPI;
using StardewValley;
using System.Linq;
using System.Collections.Generic;

namespace PamTries
{
    class PTUtilities
    {
        public static IDictionary<string, string> Lexicon { get; set; }

        public static void PopulateLexicon(IContentHelper ContentHelper)
        {
            Lexicon = ContentHelper.Load<Dictionary<string, string>>("Strings/Lexicon", ContentSource.GameContent);
        }

        public static string GetLexicon(string key, string defaultresponse = "")
        {
            string value = null;
            if (Lexicon != null)
            {
                Lexicon.TryGetValue(key, out value);
            }
            if (value == null)
            {
                if(defaultresponse == ""){ value = key; }
                else { value = defaultresponse; }
            }
            return value;
        }

        /// <summary>
        /// Checks to see if any player has a specific conversation topic. If so, gives everyone the conversation topic.
        /// </summary>
        /// <param name="ConversationTopic"></param>
        public static void SyncConversationTopics(string ConversationTopic)
        {
            if (!Game1.IsMultiplayer) { return; }
            if (!Game1.player.activeDialogueEvents.ContainsKey(ConversationTopic))
            {
                int conversationdays = -1;
                foreach (Farmer farmer in Game1.getAllFarmers())
                {
                    if (farmer.activeDialogueEvents.ContainsKey(ConversationTopic))
                    {
                        conversationdays = farmer.activeDialogueEvents[ConversationTopic];
                        break;
                    }
                }
                if (conversationdays > 0) { Game1.player.activeDialogueEvents[ConversationTopic] = conversationdays; }
            }
        }

        public static void LocalEventSyncs(IMonitor ModMonitor)
        {   //Sets Pam's home event as seen for everyone if any farmer has seen it.
            //but only if the mail flag isn't set.
            if (!Game1.IsMultiplayer) { return; }
            if (Game1.getAllFarmers().Any((Farmer farmer) => farmer.mailForTomorrow.Contains("atravita_PamTries_PennyThanks")))
            {
                Game1.player.eventsSeen.Add(99210001);
                ModMonitor.Log("Syncing event 9921001");
            }
            if (Game1.getAllFarmers().Any((Farmer farmer) => farmer.eventsSeen.Contains(99210002)))
            {
                Game1.player.eventsSeen.Add(99210002);
                ModMonitor.Log("Syncing event 99210002");
            }
        }
    }
}
