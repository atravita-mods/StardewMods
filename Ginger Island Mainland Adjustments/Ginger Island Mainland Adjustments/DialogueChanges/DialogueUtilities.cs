using GingerIslandMainlandAdjustments.Utils;

namespace GingerIslandMainlandAdjustments.DialogueChanges;

/// <summary>
/// Functions to help select the right dialogue.
/// </summary>
internal class DialogueUtilities
{
    /// <summary>
    /// Grabs a specific island dialogue for an NPC based on heart level and baseKey.
    /// Pushes it to their dialogue stack and clears previous dialogue if successful.
    /// </summary>
    /// <param name="npc">NPC.</param>
    /// <param name="baseKey">Base dialogue key.</param>
    /// <param name="hearts">heartlevel of the NPC.</param>
    /// <returns>True if dialogue found, false otherwise.</returns>
    public static bool TryGetIslandDialogue(NPC npc, string baseKey, int hearts)
    {
        string dialogueKey;
        // basekey_DayOfWeek
        dialogueKey = $"{baseKey}_{Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth)}";
        if (npc.Dialogue.ContainsKey(dialogueKey))
        {
            npc.ClearAndPushDialogue(dialogueKey);
            return true;
        }

        for (int heartLevel = Math.Max((hearts / 2) * 2, 0); heartLevel > 0; heartLevel -= 2)
        {
            // basekeyHearts
            dialogueKey = $"{baseKey}{heartLevel}";
            if (npc.Dialogue.ContainsKey(dialogueKey))
            {
                npc.ClearAndPushDialogue(dialogueKey);
                return true;
            }
        }

        if (npc.Dialogue.ContainsKey(baseKey))
        {
            // basekey
            npc.ClearAndPushDialogue(baseKey);
            return true;
        }
        Globals.ModMonitor.DebugLog($"No key found for {npc.Name} using basekey {baseKey}", LogLevel.Trace);
        return false;
    }
}
