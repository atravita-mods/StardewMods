namespace PamTries;

class PTUtilities
{
    public static IDictionary<string, string>? Lexicon { get; set; }

    public static void PopulateLexicon(IGameContentHelper contentHelper)
    {
        Lexicon = contentHelper.Load<Dictionary<string, string>>("Strings/Lexicon");
    }

    public static string GetLexicon(string key, string? defaultresponse = null)
    {
        string? value = null;
        if (Lexicon is not null)
        {
            Lexicon.TryGetValue(key, out value);
        }
        if (value is null)
        {
            if (string.IsNullOrWhiteSpace(defaultresponse))
            {
                value = key;
            }
            else
            {
                value = defaultresponse;
            }
        }
        return value;
    }

    /// <summary>
    /// Checks to see if any player has a specific conversation topic. If so, gives everyone the conversation topic.
    /// </summary>
    /// <param name="conversationTopic">conversation topic to sync.</param>
    public static void SyncConversationTopics(string conversationTopic)
    {
        if (!Game1.IsMultiplayer) { return; }
        // Rewrite this. If host has it, everyone has host's amount of days. Else, find player with it.
        if (Game1.player.activeDialogueEvents.ContainsKey(conversationTopic))
        {
            return;
        }
        if (Game1.MasterPlayer.activeDialogueEvents.TryGetValue(conversationTopic, out int conversationdays))
        {
            Game1.player.activeDialogueEvents[conversationTopic] = conversationdays;
        }
        else
        {
            foreach (Farmer farmer in Game1.getAllFarmers())
            {
                if (farmer.activeDialogueEvents.TryGetValue(conversationTopic, out conversationdays))
                {
                    Game1.player.activeDialogueEvents[conversationTopic] = conversationdays;
                }
            }
        }
    }

    public static void SyncConversationTopics(IEnumerable<string> conversationTopics)
    {
        foreach (string conversationTopic in conversationTopics)
        {
            SyncConversationTopics(conversationTopic);
        }
    }

    public static void LocalEventSyncs(IMonitor modMonitor)
    { // Sets Pam's home event as seen for everyone if any farmer has seen it.
      // but only if the mail flag isn't set.
        if (!Game1.IsMultiplayer)
        {
            return;
        }
        if (Game1.getAllFarmers().Any((Farmer farmer) => farmer.mailForTomorrow.Contains("atravita_PamTries_PennyThanks")))
        {
            Game1.player.eventsSeen.Add(99210001);
            modMonitor.Log("Syncing event 9921001");
        }
        if (Game1.getAllFarmers().Any((Farmer farmer) => farmer.eventsSeen.Contains(99210002)))
        {
            Game1.player.eventsSeen.Add(99210002);
            modMonitor.Log("Syncing event 99210002");
        }
    }
}
