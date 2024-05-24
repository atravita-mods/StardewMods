using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Objects;

namespace MoreFertilizers.HarmonyPatches.Niceties;

/// <summary>
/// Holds patches against Chests.
/// </summary>
[HarmonyPatch(typeof(Chest))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class ChestPatcher
{
    private const string HasGottenLucky = "atravita.MoreFertilizers/HasGottenLuckyFertilizer";

    [HarmonyPatch(nameof(Chest.dumpContents))]
    private static void Postfix(Chest __instance, GameLocation location)
    {
        try
        {
            if (__instance.giftbox.Value && ModEntry.LuckyFertilizerID != -1 && Game1.player.modData?.GetBool(HasGottenLucky) != true)
            {
                Game1.createMultipleObjectDebris(
                    index: ModEntry.LuckyFertilizerID,
                    xTile: (int)__instance.TileLocation.X,
                    yTile: (int)__instance.TileLocation.Y,
                    number: 5,
                    location: location);
                Game1.player.modData?.SetBool(HasGottenLucky, true);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding lucky fertilizer to gift box", ex);
        }
    }
}