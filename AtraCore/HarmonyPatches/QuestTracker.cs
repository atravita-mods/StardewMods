using System.Reflection;

using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.Reflection;

using AtraShared.ConstantsAndEnums;

using HarmonyLib;

using StardewModdingAPI.Events;

using StardewValley.Quests;

namespace AtraCore.HarmonyPatches;

/// <summary>
/// Patches to handle quests.
/// </summary>
[HarmonyPatch]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
public static class QuestTracker
{
    private const string MESSAGETYPE = "QuestTracker";
    private const string MESSAGEREMOVE = "QuestTrackerRemove";
    private const string BROADCAST = "QuestTrackerBroadcast";

    private const char SEP = 'Ω';

    private static Dictionary<long, HashSet<string>> finishedQuests = [];
    private static IMultiplayerHelper multi = null!;
    private static string uniqueID = null!;

    /// <summary>
    /// Checks if the current player has a specific quest finished.
    /// </summary>
    /// <param name="questID">ID of the quest.</param>
    /// <returns>True if quest completed, false otherwise.</returns>
    public static bool HasCompletedQuest(string questID) => HasCompletedQuest(Game1.player, questID);

    /// <summary>
    /// Checks if the current player has a specific quest finished.
    /// </summary>
    /// <param name="player">The specific player to check.</param>
    /// <param name="questID">ID of the quest.</param>
    /// <returns>True if quest completed, false otherwise.</returns>
    public static bool HasCompletedQuest(Farmer player, string questID)
        => finishedQuests.TryGetValue(player.UniqueMultiplayerID, out HashSet<string>? questSet)
            && questSet.Contains(questID);

    /// <summary>
    /// Sets up needed static fields.
    /// </summary>
    /// <param name="multiplayer">Multiplayer helper.</param>
    /// <param name="uniqueID">Unique ID.</param>
    internal static void Initialize(IMultiplayerHelper multiplayer, string uniqueID)
    {
        multi = multiplayer;
        QuestTracker.uniqueID = string.Intern(uniqueID);
    }

    /// <summary>
    /// Tracks that the current player has finished a specific quest.
    /// </summary>
    /// <param name="questID">Quest id to track.</param>
    /// <returns>True if changed, false otherwise.</returns>
    internal static bool TrackQuest(string questID) => TrackQuest(Game1.player, questID);

    /// <summary>
    /// Tracks that a specific farmer has finished a specific quest.
    /// </summary>
    /// <param name="farmer">ID of farmer.</param>
    /// <param name="questID">ID of quest.</param>
    /// <returns>True if added, false otherwise.</returns>
    internal static bool TrackQuest(Farmer farmer, string questID)
    {
        if (!finishedQuests.TryGetValue(farmer.UniqueMultiplayerID, out HashSet<string>? set))
        {
            finishedQuests[farmer.UniqueMultiplayerID] = set = [];
        }
        if (set.Add(questID))
        {
            multi.SendMessage(
                message: $"{farmer.UniqueMultiplayerID}{SEP}{questID}",
                messageType: MESSAGETYPE,
                modIDs: [uniqueID],
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
            finishedQuests = helper.ReadSaveData<Dictionary<long, HashSet<string>>>(MESSAGETYPE) ?? [];
            Broadcast();
        }
    }

    /// <summary>
    /// Resets the quest tracker.
    /// </summary>
    internal static void Reset() => finishedQuests = [];

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

    /// <inheritdoc cref="IMultiplayerEvents.PeerConnected"/>
    internal static void OnPeerConnected(PeerConnectedEventArgs e)
    {
        Broadcast(e.Peer.PlayerID);
    }

    /// <inheritdoc cref="IMultiplayerEvents.ModMessageReceived"/>
    internal static void OnMessageReceived(ModMessageReceivedEventArgs e)
    {
        static (long id, string questID)? ParseMessage(string message)
        {
            if (!message.TrySplitOnce(SEP, out ReadOnlySpan<char> first, out ReadOnlySpan<char> second))
            {
                ModEntry.ModMonitor.Log($"Received invalid message {message}", LogLevel.Error);
                return null;
            }
            if (!long.TryParse(first, out long id))
            {
                ModEntry.ModMonitor.Log($"Could not parse {first.ToString()} as unique id", LogLevel.Error);
                return null;
            }

            return (id, second.ToString());
        }

        if (e.FromModID != uniqueID)
        {
            return;
        }
        switch (e.Type)
        {
            case BROADCAST:
                finishedQuests = e.ReadAs<Dictionary<long, HashSet<string>>>();
                return;
            case MESSAGETYPE:
            {
                (long id, string questID)? pair = ParseMessage(e.ReadAs<string>());
                if (pair is not null)
                {
                    (long id, string questID) = pair.Value;
                    if (!finishedQuests.TryGetValue(id, out HashSet<string>? set))
                    {
                        finishedQuests[id] = set = [];
                    }
                    set.Add(questID);
                }
                break;
            }
            case MESSAGEREMOVE:
            {
                (long id, string questID)? pair = ParseMessage(e.ReadAs<string>());
                if (pair is not null)
                {
                    (long id, string questID) = pair.Value;
                    if (finishedQuests.TryGetValue(id, out HashSet<string>? set))
                    {
                        set.Remove(questID);
                    }
                }
                break;
            }
        }
    }

    private static void Broadcast(long? id = null)
    {
        if (Context.IsMainPlayer)
        {
            multi.SendMessage(
                message: finishedQuests,
                messageType: BROADCAST,
                modIDs: [uniqueID],
                playerIDs: id is null ? multi.GetConnectedPlayers().Where(p => !p.IsSplitScreen).Select(p => p.PlayerID).ToArray() : [id.Value]
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

    private static void Postfix(Quest __instance)
    {
        if (__instance.id?.Value is string id)
        {
            TrackQuest(id);
        }
    }

    #endregion
}
