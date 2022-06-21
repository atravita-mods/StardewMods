using HarmonyLib;
using StardewValley.Locations;

namespace MoreFertilizers.HarmonyPatches.Acquisition;

/// <summary>
/// Holds patches to add things to the museum.
/// </summary>
[HarmonyPatch(typeof(LibraryMuseum))]
internal static class MuseumRewards
{
    /// <summary>
    /// Postfix on the museum to add the prismatic fertilizer as a reward item.
    /// </summary>
    /// <param name="__instance">museum instance.</param>
    /// <param name="who">farmer.</param>
    /// <param name="__result">List of items.</param>
    [HarmonyPatch(nameof(LibraryMuseum.getRewardsForPlayer))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention.")]
    private static void Postfix(LibraryMuseum __instance, Farmer who, List<Item> __result)
    {
        if (__instance.museumPieces.Values.Contains(74) && ModEntry.PrismaticFertilizerID != -1)
        { // prismatic shard = 74
            __instance.AddRewardIfUncollected(
                who,
                __result,
                new SObject(ModEntry.PrismaticFertilizerID, 5));
        }
    }
}
