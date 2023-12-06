namespace PamTries.HarmonyPatches;

using AtraShared.Caching;
using AtraShared.ConstantsAndEnums;
using AtraShared.Utils;

using AtraShared.Utils.Extensions;

using HarmonyLib;

/// <summary>
/// Makes it so Pam doesn't like alcohol items.
/// </summary>
[HarmonyPatch(typeof(NPC))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class PamGiftTastes
{
    private static readonly TickCache<bool> HasSeenNineHeart = new(() => FarmerHelpers.GetFarmers().Any(static farmer => farmer.eventsSeen.Contains("503180")));

    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    [HarmonyPatch(nameof(NPC.getGiftTasteForThisItem))]
    private static bool PrefixGiftTastes(NPC __instance, Item item, ref int __result)
    {
        if (item is not SObject obj || __instance.Name != "Pam" || !HasSeenNineHeart.GetValue() || !obj.IsAlcoholItem())
        {
            return true;
        }

        __result = ModEntry.PamMood switch
        {
            PamMood.bad => NPC.gift_taste_like,
            PamMood.neutral => NPC.gift_taste_hate,
            _ => NPC.gift_taste_dislike,
        };

        return false;
    }
}
