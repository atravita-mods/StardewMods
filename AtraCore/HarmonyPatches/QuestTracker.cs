using System.Reflection;

using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.Reflection;

using HarmonyLib;

using StardewModdingAPI.Events;

using StardewValley.Quests;

namespace AtraCore.HarmonyPatches;

/// <summary>
/// Patches to handle quests.
/// </summary>
[HarmonyPatch]
public static class QuestTracker
{
    private const string MESSAGETYPE = "QuestTracker";
    private const string BROADCAST = "QuestTrackerBroadcast";

    private static Dictionary<long, HashSet<int>> finishedQuests = new();
    private static IMultiplayerHelper multi = null!;
    private static string uniqueID = null!;

    /// <summary>
    /// Checks if the current player has a specific quest finished.
    /// </summary>
    /// <param name="questID">ID of the quest.</param>
    /// <returns>True if quest completed, false otherwise.</returns>
    public static bool HasCompletedQuest(int questID) => HasCompletedQuest(Game1.player, questID);

    /// <summary>
    /// Checks if the current player has a specific quest finished.
    /// </summary>
    /// <param name="player">The specific player to check.</param>
    /// <param name="questID">ID of the quest.</param>
    /// <returns>True if quest completed, false otherwise.</returns>
    public static bool HasCompletedQuest(Farmer player, int questID)
        => finishedQuests.TryGetValue(player.UniqueMultiplayerID, out HashSet<int>? questSet)
            && questSet.Contains(questID);

    /// <summary>
    /// Sets up needed static fields.
    /// </summary>
    /// <param name="multiplayer">Multiplayer helper.</param>
    /// <param name="uniqueID">Unique ID.</param>
    internal static void Init(IMultiplayerHelper multiplayer, string uniqueID)
    {
        multi = multiplayer;
        QuestTracker.uniqueID = string.Intern(uniqueID);
    }

    /// <summary>
    /// Tracks that the current player has finished a specific quest.
    /// </summary>
    /// <param name="questID">Quest id to track.</param>
    /// <returns>True if changed, false otherwise.</returns>
    internal static bool TrackQuest(int questID) => TrackQuest(Game1.player, questID);

    /// <summary>
    /// Tracks that a specific farmer has finished a specific quest.
    /// </summary>
    /// <param name="farmer">ID of farmer.</param>
    /// <param name="questID">ID of quest.</param>
    /// <returns>True if added, false otherwise.</returns>
    internal static bool TrackQuest(Farmer farmer, int questID)
    {
        if (!finishedQuests.TryGetValue(farmer.UniqueMultiplayerID, out HashSet<int>? set))
        {
            finishedQuests[farmer.UniqueMultiplayerID] = set = new();
        }
        if (set.Add(questID))
        {
            multi.SendMessage(
                message: farmer.UniqueMultiplayerID + ":" + questID,
                messageType: MESSAGETYPE,
                modIDs: new[] { uniqueID },
                playerIDs: multi.GetConnectedPlayers().Where(p => !p.IsSplitScreen).Select(p => p.PlayerID).ToArray());
            return true;
        }
        return false;
    }

    /// <summary>
    /// Loads in the data from the save.
    /// </summary>
    /// <param name="helper">Data helper.</param>
    internal static void Load(IDataHelper helper)
    {
        if (Context.IsMainPlayer)
        {
            finishedQuests = helper.ReadSaveData<Dictionary<long, HashSet<int>>>(MESSAGETYPE) ?? new();
            Broadcast();
        }
    }

    /// <summary>
    /// Resets the quest tracker.
    /// </summary>
    internal static void Reset() => finishedQuests = new();

    /// <summary>
    /// Writes the quest tracker to the save.
    /// </summary>
    /// <param name="helper">SMAPI's data helper.</param>
    internal static void Write(IDataHelper helper)
    {
        if (Context.IsMainPlayer)
        {
            helper.WriteSaveData(MESSAGETYPE, finishedQuests);
        }
    }

    #region multiplayer

    internal static void OnPeerConnected(PeerConnectedEventArgs e)
    {
        Broadcast(e.Peer.PlayerID);
    }

    internal static void OnMessageReceived(ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != uniqueID)
        {
            return;
        }
        switch (e.Type)
        {
            case BROADCAST:
                finishedQuests = e.ReadAs<Dictionary<long, HashSet<int>>>();
                return;
            case MESSAGETYPE:
                string message = e.ReadAs<string>();
                if (!message.TrySplitOnce(':', out ReadOnlySpan<char> first, out ReadOnlySpan<char> second))
                {
                    ModEntry.ModMonitor.Log($"Received invalid message {message}", LogLevel.Error);
                    return;
                }
                if (!long.TryParse(first, out long id))
                {
                    ModEntry.ModMonitor.Log($"Could not parse {first.ToString()} as unique Id", LogLevel.Error);
                    return;
                }
                if (!int.TryParse(second, out int questID))
                {
                    ModEntry.ModMonitor.Log($"Could not parse {second.ToString()} as quest Id", LogLevel.Error);
                    return;
                }

                if (!finishedQuests.TryGetValue(id, out HashSet<int>? set))
                {
                    finishedQuests[id] = set = new();
                }
                set.Add(questID);
                break;
        }
    }

    private static void Broadcast(long? Id = null)
    {
        if (Context.IsMainPlayer)
        {
            multi.SendMessage(
                message: finishedQuests,
                messageType: BROADCAST,
                modIDs: new[] { uniqueID },
                playerIDs: Id is null ? multi.GetConnectedPlayers().Where(p => !p.IsSplitScreen).Select(p => p.PlayerID).ToArray() : new[] { Id.Value }
            );
        }
    }

    #endregion

    #region harmony

    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (Type t in typeof(Quest).GetAssignableTypes(publiconly: true, includeAbstract: false))
        {
            if (t.DeclaredInstanceMethodNamedOrNull(nameof(Quest.questComplete)) is MethodBase method
                && method.DeclaringType == t)
            {
                yield return method;
            }
        }
    }

    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Named for Harmony.")]
    private static void Postfix(Quest __instance)
    {
        if (__instance.id?.Value is int id)
        {
            TrackQuest(id);
        }
    }

    #endregion
}
