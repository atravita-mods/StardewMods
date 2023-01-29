using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AtraBase.Models.Result;

using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewModdingAPI.Utilities;

namespace StopRugRemoval.HarmonyPatches.Niceties;

/// <summary>
/// Fixes the secret note spawning code.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
internal static class FixSecretNotes
{
    // we're doing this as an internal optimization and intentionally not saving it in ModData
    // so it's refreshed every time the game is launched.
    private static readonly PerScreen<bool> HasSeenAllSecretNotes = new(() => false);
    private static readonly PerScreen<bool> HasSeenAllJournalScraps = new(() => false);

    private static IAssetName noteLoc = null!;

    internal static void Initialize(IGameContentHelper parser)
        => noteLoc = parser.ParseAssetName("Data/SecretNotes");

    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        if (assets is null || assets.Contains(noteLoc))
        {
            HasSeenAllSecretNotes.ResetAllScreens();
            HasSeenAllJournalScraps.ResetAllScreens();
        }
    }

    [HarmonyPatch(nameof(GameLocation.tryToCreateUnseenSecretNote))]
    private static bool Prefix(GameLocation __instance, Farmer who, ref SObject? __result)
    {
        try
        {
            __result = null;
            Option<SObject?> option = __instance.GetLocationContext() == GameLocation.LocationContext.Island
                       ? TryGenerateJournalScrap(who)
                       : TryGenerateSecretNote(who);

            if (option.IsNone)
            {
                return true;
            }
            else
            {
                __result = option.Unwrap_Or_Default();
                return false;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed while trying to generate a secret note:\n\n{ex}", LogLevel.Error);
        }

        return true;
    }

    private static Option<SObject?> TryGenerateSecretNote(Farmer who)
    {
        if (!who.hasMagnifyingGlass || HasSeenAllSecretNotes.Value)
        {
            return Option<SObject?>.None;
        }

        const string secreteNoteName = "Secret Note #";
        Dictionary<int, string> secretNoteData = Game1.content.Load<Dictionary<int, string>>(noteLoc.BaseName);

        // Get list of seen notes and add notes in inventory.
        HashSet<int> seenNotes = who.secretNotesSeen.Where(id => id < GameLocation.JOURNAL_INDEX).ToHashSet();
        foreach (var item in who.Items)
        {
            if (item.Name.StartsWith(secreteNoteName) && int.TryParse(item.Name.AsSpan(secreteNoteName.Length).Trim(), out var idx))
            {
                seenNotes.Add(idx);
            }
        }

        // find a note that the farmer has not seen.
        HashSet<int> unseenNotes = secretNoteData.Keys.Where(id => seenNotes.Contains(id)).ToHashSet();

        ModEntry.ModMonitor.DebugOnlyLog($"{unseenNotes.Count} notes unseen: {string.Join(", ", unseenNotes.Select(x => x.ToString()))}", LogLevel.Info);
        if (unseenNotes.Count == 0)
        {
            HasSeenAllSecretNotes.Value = true;
            return Option<SObject?>.None;
        }

        // copied from game code.
        double fractionOfNotesRemaining = (unseenNotes.Count - 1) / Math.Max(1f, unseenNotes.Count + seenNotes.Count - 1);
        double chanceForNewNote = GameLocation.LAST_SECRET_NOTE_CHANCE + ((GameLocation.FIRST_SECRET_NOTE_CHANCE - GameLocation.LAST_SECRET_NOTE_CHANCE) * fractionOfNotesRemaining);
        if (Game1.random.NextDouble() >= chanceForNewNote)
        {
            return new(null);
        }

        int noteID = unseenNotes.ElementAt(Game1.random.Next(unseenNotes.Count));
        SObject note = new(79, 1);
        note.Name += " #" + noteID;

        return new(note);
    }

    private static Option<SObject?> TryGenerateJournalScrap(Farmer who)
    {
        if (HasSeenAllJournalScraps.Value)
        {
            return Option<SObject?>.None;
        }

        const string journalName = "Journal Scrap #";
        Dictionary<int, string> secretNoteData = Game1.content.Load<Dictionary<int, string>>(noteLoc.BaseName);

        // get seen notes and add any note the farmer has in their inventory.
        HashSet<int> seenScraps = who.secretNotesSeen.Where(id => id >= GameLocation.JOURNAL_INDEX).ToHashSet();
        foreach (var item in who.Items)
        {
            if (item.Name.StartsWith(journalName) && int.TryParse(item.Name.AsSpan(journalName.Length).Trim(), out var idx))
            {
                seenScraps.Add(idx + GameLocation.JOURNAL_INDEX);
            }
        }

        // find a scrap that the farmer has not seen.
        HashSet<int> unseenScraps = secretNoteData.Keys.Where(id => seenScraps.Contains(id)).ToHashSet();

        ModEntry.ModMonitor.DebugOnlyLog($"{unseenScraps.Count} scraps unseen: {string.Join(", ", unseenScraps.Select(x => x.ToString()))}", LogLevel.Info);
        if (unseenScraps.Count == 0)
        {
            HasSeenAllJournalScraps.Value = true;
            return Option<SObject?>.None;
        }

        // copied from game code.
        double fractionOfNotesRemaining = (unseenScraps.Count - 1) / Math.Max(1f, unseenScraps.Count + seenScraps.Count - 1);
        double chanceForNewNote = GameLocation.LAST_SECRET_NOTE_CHANCE + ((GameLocation.FIRST_SECRET_NOTE_CHANCE - GameLocation.LAST_SECRET_NOTE_CHANCE) * fractionOfNotesRemaining);
        if (Game1.random.NextDouble() >= chanceForNewNote)
        {
            return new (null);
        }

        int scrapID = unseenScraps.Min();
        SObject note = new(842, 1);
        note.Name += " #" + (scrapID - GameLocation.JOURNAL_INDEX);

        return new(note);
    }
}
