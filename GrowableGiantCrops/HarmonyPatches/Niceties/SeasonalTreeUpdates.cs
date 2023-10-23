using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using GrowableGiantCrops.Framework;
using GrowableGiantCrops.Framework.Assets;
using GrowableGiantCrops.Framework.InventoryModels;

using HarmonyLib;

using Microsoft.Xna.Framework.Graphics;

using StardewValley.Locations;
using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops.HarmonyPatches.Niceties;

/// <summary>
/// Patches to handle updating trees seasonally.
/// </summary>
[HarmonyPatch(typeof(Tree))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class SeasonalTreeUpdates
{
    [HarmonyPrefix]
    [HarmonyPatch("loadTexture")]
    private static bool PrefixLoadTexture(Tree __instance, ref Texture2D __result)
    {
        if (!ModEntry.Config.PalmTreeBehavior.HasFlagFast(PalmTreeBehavior.Seasonal))
        {
            return true;
        }

        try
        {
            switch (__instance.treeType.Value)
            {
                case Tree.palmTree:
                {
                    GameLocation loc = __instance.currentLocation;
                    string season = loc is Desert or MineShaft ? "spring" : Game1.GetSeasonForLocation(loc);
                    if (season == "winter" && AssetCache.Get(AssetManager.WinterPalm)?.Get() is Texture2D tex)
                    {
                        __result = tex;
                        return false;
                    }
                    else if (season == "fall" && AssetCache.Get(AssetManager.FallPalm)?.Get() is Texture2D texture)
                    {
                        __result = texture;
                        return false;
                    }
                    return true;
                }
                case Tree.palmTree2:
                {
                    GameLocation loc = __instance.currentLocation;
                    string season = loc is Desert or MineShaft ? "spring" : Game1.GetSeasonForLocation(loc);
                    if (season == "winter" && AssetCache.Get(AssetManager.WinterBigPalm)?.Get() is Texture2D tex)
                    {
                        __result = tex;
                        return false;
                    }
                    else if (season == "fall" && AssetCache.Get(AssetManager.FallBigPalm)?.Get() is Texture2D texture)
                    {
                        __result = texture;
                        return false;
                    }
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("overwriting tree textures", ex);
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Tree.dayUpdate))]
    private static void PrefixDayUpdate(Tree __instance, GameLocation environment)
    {
        try
        {
            if (!ModEntry.Config.PalmTreeBehavior.HasFlagFast(PalmTreeBehavior.Stump) || __instance.health.Value <= -100f || environment is Desert or MineShaft or IslandLocation
                || __instance.modData?.ContainsKey(InventoryTree.ModDataKey) != true)
            {
                return;
            }

            if (__instance.treeType.Value is Tree.palmTree or Tree.palmTree2)
            {
                if (Game1.GetSeasonForLocation(__instance.currentLocation) == "winter")
                {
                    __instance.stump.Value = true;
                }
                else if (Game1.dayOfMonth <= 1 && Game1.IsSpring)
                {
                    __instance.stump.Value = false;
                    __instance.health.Value = 10f;
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("updating tree texture", ex);
        }
    }
}
