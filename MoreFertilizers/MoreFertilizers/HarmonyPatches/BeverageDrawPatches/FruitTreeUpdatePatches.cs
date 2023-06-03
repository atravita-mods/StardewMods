using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraCore;

using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

using MoreFertilizers.Framework;

using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches.BeverageDrawPatches;

/// <summary>
/// Patches fruit tree's draw method to add a TAS.
/// </summary>
[HarmonyPatch(typeof(FruitTree))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class FruitTreeUpdatePatches
{
    [MethodImpl(TKConstants.Hot)]
    [HarmonyPatch(nameof(FruitTree.draw))]
    private static void Postfix(FruitTree __instance)
    {
        if (!ModEntry.Config.DrawParticleEffects)
        {
            return;
        }

        try
        {
            if (__instance.modData.ContainsKey(CanPlaceHandler.MiraculousBeverages) && Singletons.Random.Next(512) == 0)
            {
                __instance.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(
                    Game1.mouseCursorsName,
                    new Rectangle(372, 1956, 10, 10),
                    new Vector2(
                        (__instance.currentTileLocation.X * 64f) + Singletons.Random.Next(-64, 96),
                        (__instance.currentTileLocation.Y * 64f) + Singletons.Random.Next(-256, -128)),
                    flipped: false,
                    0.002f,
                    Color.LimeGreen)
                {
                    alpha = 0.75f,
                    motion = new Vector2(0f, -0.5f),
                    interval = 99999f,
                    layerDepth = 1f,
                    scale = 2f,
                    scaleChange = 0.01f,
                });
            }
            else if (__instance.growthStage.Value == FruitTree.treeStage && __instance.modData.ContainsKey(CanPlaceHandler.EverlastingFruitTreeFertilizer)
                && Singletons.Random.Next(512) == 0)
            {
                Utility.addSprinklesToLocation(
                  l: __instance.currentLocation,
                  sourceXTile: (int)__instance.currentTileLocation.X,
                  sourceYTile: (int)__instance.currentTileLocation.Y - 2,
                  tilesWide: 3,
                  tilesHigh: 5,
                  totalSprinkleDuration: 400,
                  millisecondsBetweenSprinkles: 10,
                  sprinkleColor: Color.LightGoldenrodYellow);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding fruit tree sparkle effects", ex);
        }
    }
}
