using AtraBase.Toolkit.Extensions;

using AtraCore;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MoreFertilizers.Framework;

using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches.BeverageDrawPatches;

/// <summary>
/// Holds a patch to draw the Beverages effect for bushes.
/// </summary>
[HarmonyPatch(typeof(Bush))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class BushDrawPatch
{
    [HarmonyPatch(nameof(Bush.draw), new[] { typeof(SpriteBatch), typeof(Vector2) } )]
    private static void Postfix(Bush __instance)
    {
        try
        {
            if (!ModEntry.Config.DrawParticleEffects)
            {
                return;
            }

            if (__instance.modData.ContainsKey(CanPlaceHandler.MiraculousBeverages) && Singletons.Random.RollDice(512))
            {
                __instance.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(
                    Game1.mouseCursorsName,
                    new Rectangle(372, 1956, 10, 10),
                    new Vector2(
                        (__instance.currentTileLocation.X * Game1.tileSize) + Singletons.Random.Next(64),
                        (__instance.currentTileLocation.Y * Game1.tileSize) + Singletons.Random.Next(-128, 0)),
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
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("drawing bush sparkle effects", ex);
        }
    }
}
