using GrowableGiantCrops.Framework;
using GrowableGiantCrops.Framework.Assets;

using HarmonyLib;

using Microsoft.Xna.Framework.Graphics;

using StardewValley.Locations;
using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops.HarmonyPatches.Niceties;

/// <summary>
/// Patches to handle updating trees seasonally.
/// </summary>
[HarmonyPatch(typeof(Tree))]
internal static class SeasonalTreeUpdates
{
    [HarmonyPrefix]
    [HarmonyPatch("loadTexture")]
    private static bool PrefixLoadTexture(Tree __instance, ref Texture2D __result)
    {
        if (ModEntry.Config.PalmTreeBehavior != PalmTreeBehavior.Seasonal)
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
            ModEntry.ModMonitor.Log($"Failed to overwrite tree textures:\n\n{ex}", LogLevel.Error);
        }
        return true;
    }
}
