using AtraBase.Toolkit.Extensions;

using AtraCore;

using AtraShared.ConstantsAndEnums;
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
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class TreePatches
{
    [HarmonyPatch(nameof(Tree.fertilize))]
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
    private static void PostfixUpdate(Tree __instance, SObject tapper_instance)
    {
        try
        {
            if (__instance.modData?.GetBool(CanPlaceHandler.TreeTapperFertilizer) == true
                && tapper_instance.heldObject?.Value is not null && Random.Shared.RollDice(6))
            {
                ModEntry.ModMonitor.DebugOnlyLog($"Boosting tapper yield of tree at {__instance.Tile}.");
                tapper_instance.heldObject.Value.Stack++;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding tapper yield", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Tree.draw))]
    private static void PostfixDraw(Tree __instance)
    {
        try
        {
            if (ModEntry.Config.DrawParticleEffects && Utility.isOnScreen(__instance.Tile * Game1.tileSize, 256)
                && __instance.modData.ContainsKey(CanPlaceHandler.TreeTapperFertilizer) && Random.Shared.RollDice(256))
            {
                __instance.Location.temporarySprites.Add(
                    new TemporaryAnimatedSprite(
                        rowInAnimationTexture: 4,
                        position: (__instance.Tile * Game1.tileSize) + new Vector2(Random.Shared.Next(-32, 32), Random.Shared.Next(-128, -14)),
                        color: Color.Yellow,
                        animationLength: 8,
                        flipped: Random.Shared.OfChance(0.5),
                        animationInterval: 150,
                        layerDepth: ((__instance.Tile.Y * Game1.tileSize) + Random.Shared.Next(100)) / 10000f
                    )
                    {
                        scaleChange = 0.01f,
                    });
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding sparkle effects for the tree tapper fertilizer", ex);
        }
    }
}