using System.Reflection;

using AtraBase.Toolkit.Reflection;

using HarmonyLib;

using StardewValley.Quests;

namespace AtraCore.HarmonyPatches;

/// <summary>
/// Patches to handle quests.
/// </summary>
[HarmonyPatch]
public static class QuestTracker
{
    private const string MESSAGETYPE = "QuestTracker";

    private static readonly Dictionary<long, HashSet<int>> finishedQuests = new();
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
        if (__instance.id?.Value is not null or 0)
        {
            if (!finishedQuests.TryGetValue(Game1.player.UniqueMultiplayerID, out var set))
            {
                finishedQuests[Game1.player.UniqueMultiplayerID] = set = new();
            }
            set.Add(__instance.id.Value);
        }
    }

    #endregion

    internal static void Load(IDataHelper helper)
    {

    }

    internal static void Write()
    {

    }
    
    // multiplayer?
    // events?
}
