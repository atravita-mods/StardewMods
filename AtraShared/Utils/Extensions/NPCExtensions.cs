﻿// Ignore Spelling: npc basekey

using CommunityToolkit.Diagnostics;

using Microsoft.Xna.Framework;

namespace AtraShared.Utils.Extensions;

/// <summary>
/// Small extensions to Stardew's NPC class.
/// </summary>
public static class NPCExtensions
{
    /// <summary>
    /// Clears the NPC's current dialogue stack and pushes a new dialogue onto that stack.
    /// </summary>
    /// <param name="npc">NPC.</param>
    /// <param name="dialogueKey">Dialogue key.</param>
    public static void ClearAndPushDialogue(
        this NPC npc,
        string dialogueKey)
    {
        Guard.IsNotNull(npc);

        if (!string.IsNullOrWhiteSpace(dialogueKey) && npc.Dialogue.TryGetValue(dialogueKey, out string? dialogue))
        {
            // make endearment token work. This is basically copied from game code.
            dialogue = dialogue.Replace(MarriageDialogueReference.ENDEARMENT_TOKEN_LOWER, npc.getTermOfSpousalEndearment().ToLower(), StringComparison.Ordinal);
            dialogue = dialogue.Replace(MarriageDialogueReference.ENDEARMENT_TOKEN, npc.getTermOfSpousalEndearment(), StringComparison.Ordinal);

            npc.CurrentDialogue.Clear();
            npc.CurrentDialogue.Push(new Dialogue(npc, $"{npc.LoadedDialogueKey}:{dialogueKey}", dialogue) { removeOnNextMove = true });
        }
    }

    /// <summary>
    /// Tries to apply the marriage dialogue if it exists.
    /// </summary>
    /// <param name="npc">NPC in question.</param>
    /// <param name="dialogueKey">Dialogue key to search for.</param>
    /// <param name="add">To add to the stack instead of replacing.</param>
    /// <param name="clearOnMovement">To clear dialogue if the NPC moves.</param>
    /// <returns>True if successfully applied.</returns>
    public static bool TryApplyMarriageDialogueIfExisting(
        this NPC npc,
        string dialogueKey,
        bool add = false,
        bool clearOnMovement = false)
    {
        Dialogue dialogue = new MarriageDialogueReference("MarriageDialogue", dialogueKey).GetDialogue(npc);
        if (dialogue.TranslationKey is not null)
        {
            if (!add)
            {
                npc.CurrentDialogue.Clear();
                npc.currentMarriageDialogue.Clear();
            }
            dialogue.removeOnNextMove = clearOnMovement;
            npc.CurrentDialogue.Push(dialogue);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Given a base key, gets a random dialogue from a set.
    /// </summary>
    /// <param name="npc">NPC.</param>
    /// <param name="basekey">Base key to use.</param>
    /// <param name="random">Random to use, defaults to Game1.random if null.</param>
    /// <returns>null if no dialogue key found, a random dialogue key otherwise.</returns>
    public static string? GetRandomDialogue(
        this NPC npc,
        string? basekey,
        Random? random)
    {
        if (basekey is null)
        {
            return null;
        }
        if (npc.Dialogue?.Count is null or 0)
        {
            return null;
        }
        random ??= Game1.random;
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
    /// <remarks>Does **not** set _lastLoadedScheduleKey, intentionally.</remarks>
    public static bool TryGetScheduleEntry(
        this NPC npc,
        string scheduleKey,
        [NotNullWhen(returnValue: true)] out string? rawData)
    {
        rawData = null;
        Dictionary<string, string>? scheduleData = npc.getMasterScheduleRawData();
        if (scheduleData is null || scheduleKey is null)
        {
            return false;
        }
        return scheduleData.TryGetValue(scheduleKey, out rawData);
    }

    /// <summary>
    /// Gets the tile an NPC is currently facing.
    /// </summary>
    /// <param name="npc">NPC in question.</param>
    /// <returns>Tile they're facing.</returns>
    public static Vector2 GetFacingTile(this Character npc)
    {
        Vector2 tile = npc.Position / Game1.tileSize;
        return npc.facingDirection.Get() switch
        {
            Game1.up => tile - Vector2.UnitY,
            Game1.down => tile + Vector2.UnitY,
            Game1.left => tile - Vector2.UnitX,
            Game1.right => tile + Vector2.UnitX,
            _ => tile,
        };
    }

    /// <summary>
    /// Warps an npc to their default map and position.
    /// </summary>
    /// <param name="npc">NPC to warp.</param>
    public static void WarpHome(this NPC npc)
    {
        Guard.IsNotNull(npc);

        GameLocation? target = Game1.getLocationFromName(npc.DefaultMap);
        Guard.IsNotNull(target);

        Game1.warpCharacter(npc, target, npc.DefaultPosition / Game1.tileSize);
    }
}