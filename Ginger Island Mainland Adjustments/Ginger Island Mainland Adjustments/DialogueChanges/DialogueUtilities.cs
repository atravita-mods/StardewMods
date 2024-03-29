﻿using AtraShared.Utils.Extensions;
using StardewModdingAPI.Utilities;

namespace GingerIslandMainlandAdjustments.DialogueChanges;

/// <summary>
/// Functions to help select the right dialogue.
/// </summary>
internal static class DialogueUtilities
{
    private static readonly PerScreen<List<string>> DialogueLogPerScreen = new(createNewState: () => new List<string>());

    /// <summary>
    /// Gets storage for whether or not a particular dialogue string has been said today.
    /// </summary>
    private static List<string> DialogueLog => DialogueLogPerScreen.Value;

    /// <summary>
    /// Clears the dialogue log. Needs to be call per-player in splitscreen.
    /// </summary>
    internal static void ClearDialogueLog() => DialogueLog.Clear();

    /// <summary>
    /// Grabs a specific island dialogue for an NPC based on heart level and baseKey.
    /// Pushes it to their dialogue stack and clears previous dialogue if successful.
    /// </summary>
    /// <param name="npc">NPC.</param>
    /// <param name="baseKey">Base dialogue key.</param>
    /// <param name="hearts">heartlevel of the NPC.</param>
    /// <returns>True if dialogue found, false otherwise.</returns>
    internal static bool TryGetIslandDialogue(NPC npc, string baseKey, int hearts)
    {
        string dialogueKey;
        // basekey_DayOfWeek
        dialogueKey = $"{baseKey}_{Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth)}";
        if (npc.Dialogue.ContainsKey(dialogueKey))
        {
            return PushIfNotSaidAlready(npc, dialogueKey);
        }

        for (int heartLevel = Math.Max((hearts / 2) * 2, 0); heartLevel > 0; heartLevel -= 2)
        {
            // basekeyHearts
            dialogueKey = $"{baseKey}{heartLevel}";
            if (npc.Dialogue.ContainsKey(dialogueKey))
            {
                return PushIfNotSaidAlready(npc, dialogueKey);
            }
        }

        if (npc.Dialogue.ContainsKey(baseKey))
        {
            // basekey
            return PushIfNotSaidAlready(npc, baseKey);
        }
        Globals.ModMonitor.DebugOnlyLog($"No key found for {npc.Name} using basekey {baseKey}", LogLevel.Trace);
        return false;
    }

    /// <summary>
    /// If a bit of dialogue has not been said, clear the NPC's dialogue stack and add to it.
    /// Else, return false.
    /// </summary>
    /// <param name="npc">NPC.</param>
    /// <param name="dialogueKey">Dialogue key.</param>
    /// <returns>True if not seen before, false otherwise.</returns>
    private static bool PushIfNotSaidAlready(NPC npc, string dialogueKey)
    {
        string cachekey = $"{npc.Name}_{dialogueKey}";
        if (!DialogueLog.Contains(cachekey))
        {
            npc.ClearAndPushDialogue(dialogueKey);
            DialogueLog.Add(cachekey);
            return true;
        }
        return false;
    }
}
