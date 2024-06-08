using AtraBase.Models.Result;
using AtraBase.Toolkit.Extensions;

using AtraCore;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace StopRugRemoval.HarmonyPatches.Niceties;

/// <summary>
/// Fixes the secret note spawning code.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class FixSecretNotes
{
    // we're doing this as an internal optimization and intentionally not saving it in ModData
    // so it's refreshed every time the game is launched.
    private static readonly PerScreen<bool> HasSeenAllSecretNotes = new(() => false);
    private static readonly PerScreen<bool> HasSeenAllJournalScraps = new(() => false);

    private static readonly ThreadLocal<HashSet<int>> Seen = new(() => new());
    private static readonly ThreadLocal<HashSet<int>> Unseen = new(() => new());

    #region asset fussing

    private static IAssetName noteLoc = null!;

    /// <summary>
    /// Initializes the IAssetNames.
    /// </summary>
    /// <param name="parser">Game content helper.</param>
    internal static void Initialize(IGameContentHelper parser)
        => noteLoc = parser.ParseAssetName("Data/SecretNotes");

    /// <inheritdoc cref="IContentEvents.AssetsInvalidated"/>
    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        if (assets is null || assets.Contains(noteLoc))
        {
            HasSeenAllSecretNotes.ResetAllScreens();
            HasSeenAllJournalScraps.ResetAllScreens();
        }
    }

    #endregion

    #region override vanilla note creation

    [HarmonyPatch(nameof(GameLocation.tryToCreateUnseenSecretNote))]
    private static bool Prefix(GameLocation __instance, Farmer who, ref SObject? __result)
    {
        if (!ModEntry.Config.OverrideSecretNotes)
        {
            return true;
        }

        try
        {
            __result = null;
            Option<SObject?> option = __instance.InIslandContext()
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
            ModEntry.ModMonitor.LogError("generating a secret note", ex);
        }

        return true;
    }

    private static Option<SObject?> TryGenerateSecretNote(Farmer who)
    {
        if (!who.hasMagnifyingGlass || HasSeenAllSecretNotes.Value)
        {
            return Option<SObject?>.None;
        }

        const string secretNoteName = "Secret Note #";
        Dictionary<int, string> secretNoteData = DataLoader.SecretNotes(Game1.content);

        // Get list of seen notes and add notes in inventory.
        Seen.Value ??= [];
        Seen.Value.Clear();

        foreach (int id in who.secretNotesSeen)
        {
            if (id < GameLocation.JOURNAL_INDEX)
            {
                Seen.Value.Add(id);
            }
        }

        foreach (Item? item in who.Items)
        {
            if (item is not null && item.Name.StartsWith(secretNoteName) && int.TryParse(item.Name.AsSpan(secretNoteName.Length).Trim(), out int idx))
            {
                Seen.Value.Add(idx);
            }
        }

        // find a note that the farmer has not seen.
        Unseen.Value ??= [];
        Unseen.Value.Clear();
        foreach (int id in secretNoteData.Keys)
        {
            if (id < GameLocation.JOURNAL_INDEX && !Seen.Value.Contains(id))
            {
                Unseen.Value.Add(id);
            }
        }

        ModEntry.ModMonitor.DebugOnlyLog($"{Unseen.Value.Count} notes unseen: {string.Join(", ", Unseen.Value.Select(x => x.ToString()))}", LogLevel.Info);
        if (Unseen.Value.Count == 0)
        {
            HasSeenAllSecretNotes.Value = true;
            Unseen.Value.Clear();
            Seen.Value.Clear();

            return Option<SObject?>.None;
        }

        // copied from game code.
        double fractionOfNotesRemaining = (Unseen.Value.Count - 1) / Math.Max(1f, Unseen.Value.Count + Seen.Value.Count - 1);
        double chanceForNewNote = ModEntry.Config.MinNoteChance + ((ModEntry.Config.MaxNoteChance - ModEntry.Config.MinNoteChance) * fractionOfNotesRemaining);
        if (!Random.Shared.OfChance(chanceForNewNote))
        {
            return new(null);
        }

        int noteID = Unseen.Value.ElementAt(Random.Shared.Next(Unseen.Value.Count));
        SObject note = new("79", 1);
        note.Name += " #" + noteID;

        Unseen.Value.Clear();
        Seen.Value.Clear();
        return new(note);
    }

    private static Option<SObject?> TryGenerateJournalScrap(Farmer who)
    {
        if (HasSeenAllJournalScraps.Value)
        {
            return Option<SObject?>.None;
        }

        const string journalName = "Journal Scrap #";
        Dictionary<int, string> secretNoteData = DataLoader.SecretNotes(Game1.content);

        // get seen notes and add any note the farmer has in their inventory.
        Seen.Value ??= [];
        Seen.Value.Clear();

        foreach (int id in who.secretNotesSeen)
        {
            if (id >= GameLocation.JOURNAL_INDEX)
            {
                Seen.Value.Add(id);
            }
        }

        foreach (Item? item in who.Items)
        {
            if (item is not null && item.Name.StartsWith(journalName) && int.TryParse(item.Name.AsSpan(journalName.Length).Trim(), out int idx))
            {
                Seen.Value.Add(idx);
            }
        }

        // find a scrap that the farmer has not seen.
        Unseen.Value ??= [];
        Unseen.Value.Clear();
        foreach (int id in secretNoteData.Keys)
        {
            if (id >= GameLocation.JOURNAL_INDEX && !Seen.Value.Contains(id))
            {
                Unseen.Value.Add(id);
            }
        }

        ModEntry.ModMonitor.DebugOnlyLog($"{Unseen.Value.Count} scraps unseen: {string.Join(", ", Unseen.Value.Select(x => x.ToString()))}", LogLevel.Info);
        if (Unseen.Value.Count == 0)
        {
            HasSeenAllJournalScraps.Value = true;

            Unseen.Value.Clear();
            Seen.Value.Clear();
            return Option<SObject?>.None;
        }

        // copied from game code.
        double fractionOfNotesRemaining = (Unseen.Value.Count - 1) / Math.Max(1f, Unseen.Value.Count + Seen.Value.Count - 1);
        double chanceForNewNote = ModEntry.Config.MinNoteChance + ((ModEntry.Config.MaxNoteChance - ModEntry.Config.MinNoteChance) * fractionOfNotesRemaining);
        if (!Random.Shared.OfChance(chanceForNewNote))
        {
            Unseen.Value.Clear();
            Seen.Value.Clear();
            return new (null);
        }

        int scrapID = Unseen.Value.Min();
        SObject note = new("842", 1);
        note.Name += " #" + (scrapID - GameLocation.JOURNAL_INDEX);

        Unseen.Value.Clear();
        Seen.Value.Clear();
        return new(note);
    }

    #endregion
}
