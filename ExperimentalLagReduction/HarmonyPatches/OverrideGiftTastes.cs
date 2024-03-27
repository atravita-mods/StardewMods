#define TRACELOG

namespace ExperimentalLagReduction.HarmonyPatches;

using System.Collections.Concurrent;

using AtraBase.Toolkit.StringHandler;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley;

/// <summary>
/// Overrides the NPC gift tastes.
/// </summary>
[HarmonyPatch(typeof(NPC))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class OverrideGiftTastes
{
    private static readonly ConcurrentDictionary<(string NPC, string qualifiedItemID), int> Cache = new();

    private static IAssetName giftTastes = null!;

    private enum GiftPriority
    {
        None,
        Category,
        Context_Tag,
        Individual,
    }

    /// <summary>
    /// Initializes the asset names.
    /// </summary>
    /// <param name="parser">Game location helper.</param>
    internal static void Initialize(IGameContentHelper parser)
        => giftTastes = parser.ParseAssetName("Data/NPCGiftTastes");

    /// <summary>
    /// Clears the cache.
    /// </summary>
    /// <param name="assets">Assets to reset, or null to reset unconditionally.</param>
    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        if (!Cache.IsEmpty && (assets is null || assets.Contains(giftTastes)))
        {
            ModEntry.ModMonitor.Log($"Clearing gift tastes cache.");
            Cache.Clear();
        }
    }

    /// <summary>
    /// Gets the gift taste for a specific object for a specific NPC.
    /// </summary>
    /// <param name="npc">NPC to check.</param>
    /// <param name="obj">Object to check.</param>
    /// <returns>Gift tastes.</returns>
    internal static int GetGiftTaste(NPC npc, SObject obj)
    {
        int? context_taste = null;
        int? category_taste = null;

        if (obj.QualifiedItemId == "(O)StardropTea")
        {
            return NPC.gift_taste_stardroptea;
        }

        // handle individual tastes.
        if (Game1.NPCGiftTastes.TryGetValue(npc.Name, out string? taste) && !string.IsNullOrWhiteSpace(taste))
        {
            StreamSplit stream = taste.StreamSplit('/', StringSplitOptions.TrimEntries);

            // love text and values.
            if (!stream.MoveNext() || !stream.MoveNext())
            {
                ModEntry.ModMonitor.LogOnce($"Gift tastes for {npc.Name} seem broken: {taste}", LogLevel.Warn);
                goto universal;
            }

            switch (GetGiftPriority(stream.Current.Word, obj))
            {
                case GiftPriority.Individual:
                    ModEntry.ModMonitor.TraceOnlyLog($"NPC {npc.Name} loves {obj.Name} specifically");
                    return NPC.gift_taste_love;
                case GiftPriority.Category:
                    category_taste = NPC.gift_taste_love;
                    break;
                case GiftPriority.Context_Tag:
                    context_taste = NPC.gift_taste_love;
                    break;
            }

            // like text and values
            if (!stream.MoveNext() || !stream.MoveNext())
            {
                ModEntry.ModMonitor.LogOnce($"Gift tastes for {npc.Name} seem broken: {taste}", LogLevel.Warn);
                goto universal;
            }
            ReadOnlySpan<char> likes = stream.Current.Word;

            // dislikes text and values
            if (!stream.MoveNext() || !stream.MoveNext())
            {
                ModEntry.ModMonitor.LogOnce($"Gift tastes for {npc.Name} seem broken: {taste}", LogLevel.Warn);
                goto universal;
            }
            ReadOnlySpan<char> dislikes = stream.Current.Word;

            // hates text and values.
            if (!stream.MoveNext() || !stream.MoveNext())
            {
                ModEntry.ModMonitor.LogOnce($"Gift tastes for {npc.Name} seem broken: {taste}", LogLevel.Warn);
                goto universal;
            }

            switch (GetGiftPriority(stream.Current.Word, obj))
            {
                case GiftPriority.Individual:
                    ModEntry.ModMonitor.TraceOnlyLog($"NPC {npc.Name} hates {obj.Name} specifically.");
                    return NPC.gift_taste_hate;
                case GiftPriority.Category:
                    category_taste ??= NPC.gift_taste_hate;
                    break;
                case GiftPriority.Context_Tag:
                    context_taste ??= NPC.gift_taste_hate;
                    break;
            }

            // double back for likes and dislikes. God this format sucks.
            switch (GetGiftPriority(likes, obj))
            {
                case GiftPriority.Individual:
                    ModEntry.ModMonitor.TraceOnlyLog($"NPC {npc.Name} likes {obj.Name} specifically.");
                    return NPC.gift_taste_like;
                case GiftPriority.Category:
                    category_taste ??= NPC.gift_taste_like;
                    break;
                case GiftPriority.Context_Tag:
                    context_taste ??= NPC.gift_taste_like;
                    break;
            }

            switch (GetGiftPriority(dislikes, obj))
            {
                case GiftPriority.Individual:
                    ModEntry.ModMonitor.TraceOnlyLog($"NPC {npc.Name} dislikes {obj.Name} specifically.");
                    return NPC.gift_taste_dislike;
                case GiftPriority.Category:
                    category_taste ??= NPC.gift_taste_dislike;
                    break;
                case GiftPriority.Context_Tag:
                    context_taste ??= NPC.gift_taste_dislike;
                    break;
            }

            // neutrals text and values
            if (!stream.MoveNext() || !stream.MoveNext())
            {
                ModEntry.ModMonitor.LogOnce($"Gift tastes for {npc.Name} seem broken: {taste}", LogLevel.Warn);
                goto universal;
            }

            switch (GetGiftPriority(stream.Current.Word, obj))
            {
                case GiftPriority.Individual:
                    ModEntry.ModMonitor.TraceOnlyLog($"NPC {npc.Name} is neutral {obj.Name} specifically.");
                    return NPC.gift_taste_neutral;
                case GiftPriority.Category:
                    category_taste ??= NPC.gift_taste_neutral;
                    break;
                case GiftPriority.Context_Tag:
                    context_taste ??= NPC.gift_taste_neutral;
                    break;
            }

            if (context_taste.HasValue)
            {
                ModEntry.ModMonitor.TraceOnlyLog($"Gift taste for {obj.Name} for {npc.Name} was decided by context tag: {context_taste.Value}");
                return context_taste.Value;
            }
        }

universal:

        // handle universal tastes.
        if (obj.Type.Contains("Arch"))
        {
            ModEntry.ModMonitor.TraceOnlyLog($"{obj.Name} is an arch item, which is treated special.");
            return npc.Name == "Penny" || npc.Name == "Dwarf" ? NPC.gift_taste_like : NPC.gift_taste_dislike;
        }

        if (!Game1.NPCGiftTastes.TryGetValue("Universal_Love", out string? universalLoves))
        {
            ModEntry.ModMonitor.Log($"Universal loves seem missing, odd", LogLevel.Warn);
            return category_taste ?? NPC.gift_taste_neutral;
        }
        switch (GetGiftPriority(universalLoves, obj))
        {
            case GiftPriority.Individual:
                ModEntry.ModMonitor.TraceOnlyLog($"{obj.Name} is a universal love, so is loved by {npc.Name}");
                return NPC.gift_taste_love;
            case GiftPriority.Category:
                category_taste ??= NPC.gift_taste_love;
                break;
            case GiftPriority.Context_Tag:
                context_taste ??= NPC.gift_taste_love;
                break;
        }

        if (!Game1.NPCGiftTastes.TryGetValue("Universal_Hate", out string? universalHates))
        {
            ModEntry.ModMonitor.Log($"Universal hates seem missing, odd", LogLevel.Warn);
            return category_taste ?? NPC.gift_taste_neutral;
        }
        switch (GetGiftPriority(universalHates, obj))
        {
            case GiftPriority.Individual:
                ModEntry.ModMonitor.TraceOnlyLog($"{obj.Name} is a universal hate, so is hated by {npc.Name}");
                return NPC.gift_taste_hate;
            case GiftPriority.Category:
                category_taste ??= NPC.gift_taste_hate;
                break;
            case GiftPriority.Context_Tag:
                context_taste ??= NPC.gift_taste_hate;
                break;
        }

        if (!Game1.NPCGiftTastes.TryGetValue("Universal_Like", out string? universalLikes))
        {
            ModEntry.ModMonitor.Log($"Universal likes seem missing, odd", LogLevel.Warn);
            return category_taste ?? NPC.gift_taste_neutral;
        }
        switch (GetGiftPriority(universalLikes, obj))
        {
            case GiftPriority.Individual:
                ModEntry.ModMonitor.TraceOnlyLog($"{obj.Name} is a universal like, so is liked by {npc.Name}");
                return NPC.gift_taste_like;
            case GiftPriority.Category:
                category_taste ??= NPC.gift_taste_like;
                break;
            case GiftPriority.Context_Tag:
                context_taste ??= NPC.gift_taste_like;
                break;
        }

        if (!Game1.NPCGiftTastes.TryGetValue("Universal_Dislike", out string? universalDislike))
        {
            ModEntry.ModMonitor.Log($"Universal dislikes seem missing, odd", LogLevel.Warn);
            return category_taste ?? NPC.gift_taste_neutral;
        }
        switch (GetGiftPriority(universalDislike, obj))
        {
            case GiftPriority.Individual:
                ModEntry.ModMonitor.TraceOnlyLog($"{obj.Name} is a universal dislike, so is disliked by {npc.Name}");
                return NPC.gift_taste_dislike;
            case GiftPriority.Category:
                category_taste ??= NPC.gift_taste_dislike;
                break;
            case GiftPriority.Context_Tag:
                context_taste ??= NPC.gift_taste_dislike;
                break;
        }

        if (!Game1.NPCGiftTastes.TryGetValue("Universal_Neutral", out string? universalNeutrals))
        {
            ModEntry.ModMonitor.Log($"Universal neutrals seem missing, odd", LogLevel.Warn);
            return category_taste ?? NPC.gift_taste_neutral;
        }
        switch (GetGiftPriority(universalNeutrals, obj))
        {
            case GiftPriority.Individual:
                ModEntry.ModMonitor.TraceOnlyLog($"{obj.Name} is a universal neutral, so is neutral to {npc.Name}");
                return NPC.gift_taste_neutral;
            case GiftPriority.Category:
                category_taste ??= NPC.gift_taste_neutral;
                break;
            case GiftPriority.Context_Tag:
                context_taste ??= NPC.gift_taste_neutral;
                break;
        }

        if (context_taste.HasValue)
        {
            ModEntry.ModMonitor.TraceOnlyLog($"Gift taste for {obj.Name} for {npc.Name} was decided by universal context tag: {context_taste.Value}");
            return context_taste.Value;
        }

        if (category_taste.HasValue)
        {
            ModEntry.ModMonitor.TraceOnlyLog($"Gift taste for {obj.Name} for {npc.Name} was decided by category: {category_taste.Value}");
            return category_taste.Value;
        }

        if (obj.Edibility != -300 && obj.Edibility < 0)
        {
            return NPC.gift_taste_hate;
        }
        else if (obj.Price < 20)
        {
            return NPC.gift_taste_dislike;
        }

        return NPC.gift_taste_neutral;
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(nameof(NPC.getGiftTasteForThisItem))]
    private static bool GetGiftTastePrefix(NPC __instance, Item item, ref int __result)
    {
        if (!ModEntry.Config.OverrideGiftTastes)
        {
            return true;
        }

        __result = NPC.gift_taste_neutral;
        if (item is not SObject obj)
        {
            return false;
        }

        if (ModEntry.Config.UseGiftTastesCache && Cache.TryGetValue((__instance.Name, obj.QualifiedItemId), out int cacheTaste))
        {
            ModEntry.ModMonitor.TraceOnlyLog($"Got gift taste for {obj.Name} ({obj.QualifiedItemId}) for {__instance.Name} from cache.");
            __result = cacheTaste;
            return false;
        }

        try
        {
            __result = GetGiftTaste(__instance, obj);
            if (ModEntry.Config.UseGiftTastesCache)
            {
                Cache[(__instance.Name, item.QualifiedItemId)] = __result;
            }
            return false;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"overriding gift tastes for {item.QualifiedItemId}", ex);
        }
        return true;
    }

    /// <summary>
    /// Given a list of tastes, look up the best match for this item.
    /// </summary>
    /// <param name="gifts">Gift list to look at.</param>
    /// <param name="obj">Object to look at.</param>
    /// <returns>Highest priority gift taste. Goes in order of Individual->ContextTag->Category->None.</returns>
    private static GiftPriority? GetGiftPriority(ReadOnlySpan<char> gifts, SObject obj)
    {
        GiftPriority priority = GiftPriority.None;
        foreach (SpanSplitEntry giftItem in gifts.StreamSplit(null, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (giftItem.Word.Length == 0)
            {
                continue;
            }

            if (giftItem.Word.Equals(obj.ItemId, StringComparison.Ordinal))
            {
                return GiftPriority.Individual;
            }

            switch (priority)
            {
                case GiftPriority.None:
                {
                    if(int.TryParse(giftItem, out int itemID) && itemID == obj.Category)
                    {
                        priority = GiftPriority.Category;
                        break;
                    }
                    goto case GiftPriority.Category;
                }
                case GiftPriority.Category:
                {
                    if (obj.HasContextTag(giftItem.ToString()))
                    {
                        priority = GiftPriority.Context_Tag;
                    }
                    break;
                }
            }
        }

        return priority;
    }
}
