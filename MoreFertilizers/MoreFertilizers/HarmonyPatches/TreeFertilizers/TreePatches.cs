using AtraShared.Utils.Extensions;
using HarmonyLib;

using Microsoft.Xna.Framework;

using MoreFertilizers.Framework;
using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches.TreeFertilizers;

/// <summary>
/// Patches against Tree.
/// </summary>
[HarmonyPatch(typeof(Tree))]
internal static class TreePatches
{
    [HarmonyPatch(nameof(Tree.fertilize))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "HarmonyConvention")]
    private static void Postfix(Tree __instance, bool __result)
    {
        if (__result)
        {
            __instance.modData?.SetBool(CanPlaceHandler.TreeFertilizer, true);
        }
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.LowerThanNormal)]
    [HarmonyPatch(nameof(Tree.UpdateTapperProduct))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "HarmonyConvention")]
    private static void PostfixUpdate(Tree __instance, SObject tapper_instance)
    {
        if (__instance.modData?.GetBool(CanPlaceHandler.TreeTapperFertilizer) == true)
        {
            ModEntry.ModMonitor.DebugOnlyLog($"Reducing tapper time of tree at {__instance.currentTileLocation}.");
            tapper_instance.MinutesUntilReady = Math.Max((int)(tapper_instance.MinutesUntilReady * 0.75), 0);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Tree.draw))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "HarmonyConvention")]
    private static void PostfixDraw(Tree __instance)
    {
        if (__instance.modData.ContainsKey(CanPlaceHandler.TreeTapperFertilizer) && Game1.random.Next(512) == 0)
        {
            __instance.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(
                Game1.mouseCursorsName,
                new Rectangle(372, 1956, 10, 10),
                new Vector2(
                    (__instance.currentTileLocation.X * 64f) + Game1.random.Next(-64, 96),
                    (__instance.currentTileLocation.Y * 64f) + Game1.random.Next(-256, -128)),
                flipped: false,
                0.002f,
                Color.LightGoldenrodYellow)
            {
                alpha = 0.75f,
                motion = new Vector2(0f, -0.5f),
                interval = 99999f,
                layerDepth = 1f,
                scale = 2f,
                scaleChange = 0.01f,
            });
        }
    }
}