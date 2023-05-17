#define TRACELOG

using AtraBase.Models.Result;
using AtraBase.Toolkit.StringHandler;

using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley;

namespace ExperimentalLagReduction.HarmonyPatches;

[HarmonyPatch(typeof(NPC))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention.")]
internal static class OverrideGiftTastes
{
    private enum GiftPriority
    {
        None,
        Category,
        Context_Tag,
        Individual,
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(NPC.getGiftTasteForThisItem))]
    [HarmonyPriority(Priority.Last)]
    private static bool GetGiftTastePrefix(NPC __instance, Item item, ref int __result)
    {
        __result = NPC.gift_taste_neutral;
        if (item is not SObject obj)
        {
            return false;
        }

        int? context_taste = null;
        int? category_taste = null;

        // handle individual tastes.
        if (Game1.NPCGiftTastes.TryGetValue(__instance.Name, out var taste) && !string.IsNullOrWhiteSpace(taste))
        {
            var stream = taste.StreamSplit('/', StringSplitOptions.TrimEntries);

            // love text and values.
            if (!stream.MoveNext() || !stream.MoveNext())
            {
                ModEntry.ModMonitor.LogOnce($"Gift tastes for {__instance.Name} seem broken: {taste}");
                goto universal;
            }

            switch (GetGiftPriority(stream.Current.Word.StreamSplit(null, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), obj))
            {
                case GiftPriority.Individual:
                    ModEntry.ModMonitor.TraceOnlyLog($"NPC {__instance.Name} loves {obj.Name} specifically");
                    __result = NPC.gift_taste_love;
                    return false;
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
                ModEntry.ModMonitor.LogOnce($"Gift tastes for {__instance.Name} seem broken: {taste}");
                goto universal;
            }
            var likes = stream.Current.Word.StreamSplit();

            // dislikes text and values
            if (!stream.MoveNext() || !stream.MoveNext())
            {
                ModEntry.ModMonitor.LogOnce($"Gift tastes for {__instance.Name} seem broken: {taste}");
                goto universal;
            }
            var dislikes = stream.Current.Word.StreamSplit();

            // hates text and values.
            if (!stream.MoveNext() || !stream.MoveNext())
            {
                ModEntry.ModMonitor.LogOnce($"Gift tastes for {__instance.Name} seem broken: {taste}");
                goto universal;
            }

            switch (GetGiftPriority(stream.Current.Word.StreamSplit(), obj))
            {
                case GiftPriority.Individual:
                    ModEntry.ModMonitor.TraceOnlyLog($"NPC {__instance.Name} hates {obj.Name} specifically.");
                    __result = NPC.gift_taste_hate;
                    return false;
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
                    ModEntry.ModMonitor.TraceOnlyLog($"NPC {__instance.Name} likes {obj.Name} specifically.");
                    __result = NPC.gift_taste_like;
                    return false;
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
                    ModEntry.ModMonitor.TraceOnlyLog($"NPC {__instance.Name} dislikes {obj.Name} specifically.");
                    __result = NPC.gift_taste_dislike;
                    return false;
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
                ModEntry.ModMonitor.LogOnce($"Gift tastes for {__instance.Name} seem broken: {taste}");
                goto universal;
            }

            switch (GetGiftPriority(stream.Current.Word.StreamSplit(), obj))
            {
                case GiftPriority.Individual:
                    ModEntry.ModMonitor.TraceOnlyLog($"NPC {__instance.Name} is neutral {obj.Name} specifically.");
                    __result = NPC.gift_taste_neutral;
                    return false;
                case GiftPriority.Category:
                    category_taste ??= NPC.gift_taste_neutral;
                    break;
                case GiftPriority.Context_Tag:
                    context_taste ??= NPC.gift_taste_neutral;
                    break;
            }

            if (context_taste.HasValue)
            {
                ModEntry.ModMonitor.TraceOnlyLog($"Gift taste for {obj.Name} for {__instance.Name} was decided by context tag: {context_taste.Value}");
                __result = context_taste.Value;
                return false;
            }
        }

universal:
        if (obj.Type.Contains("Arch"))
        {
            ModEntry.ModMonitor.TraceOnlyLog($"{obj.Name} is an arch item, which is treated special.");
            if (__instance.Name == "Penny" || __instance.Name == "Dwarf")
            {
                __result = NPC.gift_taste_like;
                return false;
            }
            __result = NPC.gift_taste_dislike;
            return false;
        }

        if (!Game1.NPCGiftTastes.TryGetValue("Universal_Love", out var universalLoves))
        {
            ModEntry.ModMonitor.Log($"Universal loves seem missing, odd", LogLevel.Warn);
            return false;
        }
        switch (GetGiftPriority(universalLoves.StreamSplit(), obj))
        {
            case GiftPriority.Individual:
                ModEntry.ModMonitor.DebugOnlyLog($"{obj.Name} is a universal love, so is loved by {__instance.Name}");
                __result = NPC.gift_taste_love;
                return false;
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
            return false;
        }
        switch (GetGiftPriority(universalHates.StreamSplit(), obj))
        {
            case GiftPriority.Individual:
                ModEntry.ModMonitor.DebugOnlyLog($"{obj.Name} is a universal hate, so is hated by {__instance.Name}");
                __result = NPC.gift_taste_hate;
                return false;
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
            return false;
        }
        switch (GetGiftPriority(universalLikes.StreamSplit(), obj))
        {
            case GiftPriority.Individual:
                ModEntry.ModMonitor.DebugOnlyLog($"{obj.Name} is a universal like, so is liked by {__instance.Name}");
                __result = NPC.gift_taste_like;
                return false;
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
            return false;
        }
        switch (GetGiftPriority(universalDislike.StreamSplit(), obj))
        {
            case GiftPriority.Individual:
                ModEntry.ModMonitor.DebugOnlyLog($"{obj.Name} is a universal dislike, so is disliked by {__instance.Name}");
                __result = NPC.gift_taste_dislike;
                return false;
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
            return false;
        }
        switch (GetGiftPriority(universalNeutrals.StreamSplit(), obj))
        {
            case GiftPriority.Individual:
                ModEntry.ModMonitor.DebugOnlyLog($"{obj.Name} is a universal neutral, so is neutral to {__instance.Name}");
                __result = NPC.gift_taste_neutral;
                return false;
            case GiftPriority.Category:
                category_taste ??= NPC.gift_taste_neutral;
                break;
            case GiftPriority.Context_Tag:
                context_taste ??= NPC.gift_taste_neutral;
                break;
        }

        if (context_taste.HasValue)
        {
            ModEntry.ModMonitor.TraceOnlyLog($"Gift taste for {obj.Name} for {__instance.Name} was decided by universal context tag: {context_taste.Value}");
            __result = context_taste.Value;
            return false;
        }

        if (category_taste.HasValue)
        {
            ModEntry.ModMonitor.TraceOnlyLog($"Gift taste for {obj.Name} for {__instance.Name} was decided by category: {category_taste.Value}");
            __result = category_taste.Value;
            return false;
        }

        if (obj.Edibility != -300 && obj.Edibility < 0)
        {
            __result = NPC.gift_taste_hate;
        }
        else if (obj.Price < 20)
        {
            __result = NPC.gift_taste_dislike;
        }

        return false;
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
