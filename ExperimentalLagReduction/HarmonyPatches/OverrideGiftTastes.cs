#define TRACELOG

using System.Collections.Concurrent;

using AtraBase.Toolkit.StringHandler;

using AtraShared.Utils.Extensions;

using HarmonyLib;

namespace ExperimentalLagReduction.HarmonyPatches;

/// <summary>
/// Overrides the NPC gift tastes.
/// </summary>
[HarmonyPatch(typeof(NPC))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention.")]
internal static class OverrideGiftTastes
{
    private static IAssetName giftTastes = null!;

    private static readonly ConcurrentDictionary<(string NPC, int itemID), int> _cache = new();

    private enum GiftPriority
    {
        None,
        Category,
        Context_Tag,
        Individual,
    }

    internal static void Initialize(IGameContentHelper parser)
        => parser.ParseAssetName("Data/NPCGiftTastes");

    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        if (assets is null || assets.Contains(giftTastes))
        {
            ModEntry.ModMonitor.Log($"Clearing gift tastes cache.");
            _cache.Clear();
        }
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

        if (_cache.TryGetValue((__instance.Name, obj.ParentSheetIndex), out var cacheTaste))
        {
            ModEntry.ModMonitor.TraceOnlyLog($"Got gift taste for {obj.Name} for {__instance.Name} from cache.");
            __result = cacheTaste;
            return false;
        }

        __result = GetGiftTaste(__instance, obj);
        return false;
    }

    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(nameof(NPC.getGiftTasteForThisItem))]
    private static void Postfix(NPC __instance, Item item, ref int __result)
    {
        if (item is SObject)
        {
            _cache[(__instance.Name, item.ParentSheetIndex)] = __result;
        }
    }

    internal static int GetGiftTaste(NPC npc, SObject obj)
    {
        int? context_taste = null;
        int? category_taste = null;

        // handle individual tastes.
        if (Game1.NPCGiftTastes.TryGetValue(npc.Name, out var taste) && !string.IsNullOrWhiteSpace(taste))
        {
            StreamSplit stream = taste.StreamSplit('/', StringSplitOptions.TrimEntries);

            // love text and values.
            if (!stream.MoveNext() || !stream.MoveNext())
            {
                ModEntry.ModMonitor.LogOnce($"Gift tastes for {npc.Name} seem broken: {taste}");
                goto universal;
            }

            switch (GetGiftPriority(stream.Current.Word.StreamSplit(null, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), obj))
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
                ModEntry.ModMonitor.LogOnce($"Gift tastes for {npc.Name} seem broken: {taste}");
                goto universal;
            }
            var likes = stream.Current.Word.StreamSplit();

            // dislikes text and values
            if (!stream.MoveNext() || !stream.MoveNext())
            {
                ModEntry.ModMonitor.LogOnce($"Gift tastes for {npc.Name} seem broken: {taste}");
                goto universal;
            }
            var dislikes = stream.Current.Word.StreamSplit();

            // hates text and values.
            if (!stream.MoveNext() || !stream.MoveNext())
            {
                ModEntry.ModMonitor.LogOnce($"Gift tastes for {npc.Name} seem broken: {taste}");
                goto universal;
            }

            switch (GetGiftPriority(stream.Current.Word.StreamSplit(), obj))
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
                ModEntry.ModMonitor.LogOnce($"Gift tastes for {npc.Name} seem broken: {taste}");
                goto universal;
            }

            switch (GetGiftPriority(stream.Current.Word.StreamSplit(), obj))
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
        if (obj.Type.Contains("Arch"))
        {
            ModEntry.ModMonitor.TraceOnlyLog($"{obj.Name} is an arch item, which is treated special.");
            return npc.Name == "Penny" || npc.Name == "Dwarf" ? NPC.gift_taste_like : NPC.gift_taste_dislike;
        }

        if (!Game1.NPCGiftTastes.TryGetValue("Universal_Love", out var universalLoves))
        {
            ModEntry.ModMonitor.Log($"Universal loves seem missing, odd", LogLevel.Warn);
            return category_taste ?? NPC.gift_taste_neutral;
        }
        switch (GetGiftPriority(universalLoves.StreamSplit(), obj))
        {
            case GiftPriority.Individual:
                ModEntry.ModMonitor.DebugOnlyLog($"{obj.Name} is a universal love, so is loved by {npc.Name}");
                return NPC.gift_taste_love;
            case GiftPriority.Category:
                category_taste ??= NPC.gift_taste_love;
                break;
            case GiftPriority.Context_Tag:
                context_taste ??= NPC.gift_taste_love;
                break;
        }

        if (!Game1.NPCGiftTastes.TryGetValue("Universal_Hate", out var universalHates))
        {
            ModEntry.ModMonitor.Log($"Universal hates seem missing, odd", LogLevel.Warn);
            return category_taste ?? NPC.gift_taste_neutral;
        }
        switch (GetGiftPriority(universalHates.StreamSplit(), obj))
        {
            case GiftPriority.Individual:
                ModEntry.ModMonitor.DebugOnlyLog($"{obj.Name} is a universal hate, so is hated by {npc.Name}");
                return NPC.gift_taste_hate;
            case GiftPriority.Category:
                category_taste ??= NPC.gift_taste_hate;
                break;
            case GiftPriority.Context_Tag:
                context_taste ??= NPC.gift_taste_hate;
                break;
        }

        if (!Game1.NPCGiftTastes.TryGetValue("Universal_Like", out var universalLikes))
        {
            ModEntry.ModMonitor.Log($"Universal likes seem missing, odd", LogLevel.Warn);
            return category_taste ?? NPC.gift_taste_neutral;
        }
        switch (GetGiftPriority(universalLikes.StreamSplit(), obj))
        {
            case GiftPriority.Individual:
                ModEntry.ModMonitor.DebugOnlyLog($"{obj.Name} is a universal like, so is liked by {npc.Name}");
                return NPC.gift_taste_like;
            case GiftPriority.Category:
                category_taste ??= NPC.gift_taste_like;
                break;
            case GiftPriority.Context_Tag:
                context_taste ??= NPC.gift_taste_like;
                break;
        }

        if (!Game1.NPCGiftTastes.TryGetValue("Universal_Dislike", out var universalDislike))
        {
            ModEntry.ModMonitor.Log($"Universal dislikes seem missing, odd", LogLevel.Warn);
            return category_taste ?? NPC.gift_taste_neutral;
        }
        switch (GetGiftPriority(universalDislike.StreamSplit(), obj))
        {
            case GiftPriority.Individual:
                ModEntry.ModMonitor.DebugOnlyLog($"{obj.Name} is a universal dislike, so is disliked by {npc.Name}");
                return NPC.gift_taste_dislike;
            case GiftPriority.Category:
                category_taste ??= NPC.gift_taste_dislike;
                break;
            case GiftPriority.Context_Tag:
                context_taste ??= NPC.gift_taste_dislike;
                break;
        }

        if (!Game1.NPCGiftTastes.TryGetValue("Universal_Neutral", out var universalNeutrals))
        {
            ModEntry.ModMonitor.Log($"Universal neutrals seem missing, odd", LogLevel.Warn);
            return category_taste ?? NPC.gift_taste_neutral;
        }
        switch (GetGiftPriority(universalNeutrals.StreamSplit(), obj))
        {
            case GiftPriority.Individual:
                ModEntry.ModMonitor.DebugOnlyLog($"{obj.Name} is a universal neutral, so is neutral to {npc.Name}");
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

    /// <summary>
    /// Given a list of tastes, look up the best match for this item.
    /// </summary>
    /// <param name="giftList">Gift list to look at.</param>
    /// <param name="obj">Object to look at.</param>
    /// <returns>Highest priority gift taste. Goes in order of Individual->ContextTag->Category->None.</returns>
    private static GiftPriority? GetGiftPriority(StreamSplit giftList, SObject obj)
    {
        GiftPriority priority = GiftPriority.None;
        foreach (SpanSplitEntry giftItem in giftList)
        {
            if (giftItem.Word.Length == 0)
            {
                continue;
            }

            if (int.TryParse(giftItem, out int loveID))
            {
                if (loveID >= 0)
                {
                    if (loveID == obj.ParentSheetIndex)
                    {
                        return GiftPriority.Individual;
                    }
                }
                else if (loveID == obj.Category && priority == GiftPriority.None)
                {
                    priority = GiftPriority.Category;
                }
            }
            else if (priority < GiftPriority.Context_Tag && obj.HasContextTag(giftItem.ToString()))
            {
                priority = GiftPriority.Context_Tag;
            }
        }

        return priority;
    }
}
