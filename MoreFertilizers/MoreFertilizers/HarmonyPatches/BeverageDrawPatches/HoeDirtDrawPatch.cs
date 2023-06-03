using AtraBase.Toolkit.Extensions;

using AtraCore;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches.BeverageDrawPatches;

/// <summary>
/// A patch on hoedirt to draw in the particle effects for the beverage fertilizer.
/// </summary>
[HarmonyPatch(typeof(HoeDirt))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal class HoeDirtDrawPatch
{
    [HarmonyPatch(nameof(HoeDirt.draw))]
    private static void Postfix(HoeDirt __instance)
    {
        if (!ModEntry.Config.DrawParticleEffects)
        {
            return;
        }

        try
        {
            if (__instance.fertilizer.Value != -1 && __instance.fertilizer.Value == ModEntry.MiraculousBeveragesID && Singletons.Random.RollDice(512))
            {
                __instance.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(
                    Game1.mouseCursorsName,
                    new Rectangle(372, 1956, 10, 10),
                    new Vector2(
                        (__instance.currentTileLocation.X * 64f) + Singletons.Random.Next(32),
                        (__instance.currentTileLocation.Y * 64f) + Singletons.Random.Next(-128, 0)),
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
            ModEntry.ModMonitor.LogError("drawing hoe dirt sparkle effects", ex);
        }
    }
}
