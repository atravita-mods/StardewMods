using HarmonyLib;
using Microsoft.Xna.Framework;

namespace ReviveDeadCrops.HarmonyPatches;

/// <summary>
/// Patches Utility to draw the green square when needed.
/// </summary>
[HarmonyPatch(typeof(Utility))]
internal static class UtilityPatcher
{
    [HarmonyPriority(Priority.High)]
    [HarmonyPatch(nameof(Utility.isThereAnObjectHereWhichAcceptsThisItem))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention")]
    private static bool Prefix(GameLocation location, Item item, int x, int y, Farmer f, ref bool __result)
    {
        try
        {
            Vector2 tile = new(MathF.Floor(x / 64f), MathF.Floor(y / 64f));
            if (Utility.withinRadiusOfPlayer(x, y, 2, f) && item is SObject obj && ModEntry.Api.CanApplyDust(location, tile, obj))
            {
                __result = true;
                return false;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Attempt to prefix Utility.playerCanPlaceItemHere has failed:\n\n{ex}", LogLevel.Error);
        }
        return true;
    }
}
