using System.Runtime.CompilerServices;

using AtraBase.Toolkit;
using AtraBase.Toolkit.Extensions;

using AtraCore;

using AtraShared.ConstantsAndEnums;
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
        if (!ModEntry.Config.DrawParticleEffects || !Utility.isOnScreen(__instance.Tile * Game1.tileSize, 256))
        {
            return;
        }

        try
        {
            if (__instance.modData.ContainsKey(CanPlaceHandler.MiraculousBeverages) && Random.Shared.RollDice(512))
            {
                __instance.Location.TemporarySprites.Add(new TemporaryAnimatedSprite(
                    Game1.mouseCursorsName,
                    new Rectangle(372, 1956, 10, 10),
                    new Vector2(
                        (__instance.Tile.X * Game1.tileSize) + Random.Shared.Next(-64, 96),
                        (__instance.Tile.Y * Game1.tileSize) + Random.Shared.Next(-256, -128)),
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
                && Random.Shared.RollDice(512))
            {
                Utility.addSprinklesToLocation(
                  l: __instance.Location,
                  sourceXTile: (int)__instance.Tile.X,
                  sourceYTile: (int)__instance.Tile.Y - 2,
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
