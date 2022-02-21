using System.Diagnostics.CodeAnalysis;
using NotNullAttribute = System.Diagnostics.CodeAnalysis.NotNullAttribute;

namespace AtraShared.Utils.Extensions;

/// <summary>
/// Small extensions to Stardew's NPC class.
/// </summary>
internal static class NPCExtensions
{
    /// <summary>
    /// Clears the NPC's current dialogue stack and pushes a new dialogue onto that stack.
    /// </summary>
    /// <param name="npc">NPC.</param>
    /// <param name="dialogueKey">Dialogue key.</param>
    public static void ClearAndPushDialogue(
        [NotNull] this NPC npc,
        [NotNull] string dialogueKey)
    {
        npc.CurrentDialogue.Clear();
        npc.CurrentDialogue.Push(new Dialogue(npc.Dialogue[dialogueKey], npc) { removeOnNextMove = true });
    }

    /// <summary>
    /// Given a base key, gets a random dialogue from a set.
    /// </summary>
    /// <param name="npc">NPC.</param>
    /// <param name="basekey">Basekey to use.</param>
    /// <param name="random">Random to use, defaults to Game1.random if null.</param>
    /// <returns>null if no dialogue key found, a random dialogue key otherwise.</returns>
    public static string? GetRandomDialogue(
        [NotNull] this NPC npc,
        string? basekey,
        Random? random)
    {
        if (basekey is null)
        {
            return null;
        }
        if (random is null)
        {
            random = Game1.random;
        }
        if (npc.Dialogue.ContainsKey(basekey))
        {
            int index = 1;
            while (npc.Dialogue.ContainsKey($"{basekey}_{++index}"))
            {
            }
            int selection = random.Next(1, index);
            return (selection == 1) ? basekey : $"{basekey}_{selection}";
        }
        return null;
    }

    /// <summary>
    /// Helper method to get an NPC's raw schedule string for a specific key.
    /// </summary>
    /// <param name="npc">NPC in question.</param>
    /// <param name="scheduleKey">Schedule key to look for.</param>
    /// <param name="rawData">Raw schedule string.</param>
    /// <returns>True if successful, false otherwise.</returns>
    /// <remarks>Does **not** set _lastLoadedScheduleKey.</remarks>
    public static bool TryGetScheduleEntry(
        [NotNull] this NPC npc,
        [NotNull] string scheduleKey,
        [NotNullWhen(returnValue: true)] out string? rawData)
    {
        rawData = null;
        Dictionary<string, string> scheduleData = npc.getMasterScheduleRawData();
        if (scheduleData is null)
        {
            return false;
        }
        return scheduleData.TryGetValue(scheduleKey, out rawData);
    }
}