using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using IdentifiableCombinedRings.DataModels;
using IdentifiableCombinedRings.Framework;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Netcode;

using StardewValley.Objects;

namespace IdentifiableCombinedRings.HarmonyPatches;

/// <summary>
/// Holds patches on combined rings.
/// </summary>
[HarmonyPatch(typeof(CombinedRing))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal class CombinedRingPatcher
{
    /// <inheritdoc cref="CombinedRing.drawInMenu(SpriteBatch, Vector2, float, float, float, StackDrawType, Color, bool)"/>
    /// <param name="__instance">Combined ring to check.</param>
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Low)]
    [MethodImpl(TKConstants.Hot)]
    [HarmonyPatch(nameof(CombinedRing.drawInMenu))]
    public static bool PrefixGetDisplayName(
        CombinedRing __instance,
        SpriteBatch spriteBatch,
        Vector2 location,
        float scaleSize,
        float transparency,
        float layerDepth,
        StackDrawType drawStackNumber,
        Color color,
        bool drawShadow)
    {
        if (!ModEntry.Config.OverrideCombinedRing)
        {
            return true;
        }

        try
        {
            NetList<Ring, NetRef<Ring>> combinedRings = __instance.combinedRings;
            if (combinedRings.Count <= 1 || combinedRings.Count > 2 || combinedRings[0] is CombinedRing || combinedRings[1] is CombinedRing)
            {
                return true;
            }

            string first = combinedRings[0].ItemId;
            string second = combinedRings[1].ItemId;

            if (AssetManager.GetOverrideTexture(first, second) is Texture2D texture)
            {
                spriteBatch.Draw(
                    texture,
                    location + (new Vector2(32f, 32f) * scaleSize),
                    new Rectangle(0, 0, 16, 16),
                    color * transparency,
                    0f,
                    new Vector2(8f, 8f) * scaleSize,
                    scaleSize * Game1.pixelZoom,
                    SpriteEffects.None,
                    layerDepth);
                return false;
            }

            const float scaleAdjustment = 0.8f;

            combinedRings[0].drawInMenu(
                spriteBatch,
                location + new Vector2(8f, 0f),
                scaleSize * scaleAdjustment,
                transparency,
                layerDepth,
                drawStackNumber,
                color,
                drawShadow);
            combinedRings[1].drawInMenu(
                spriteBatch,
                location + new Vector2(-16f, 12f),
                scaleSize * scaleAdjustment,
                transparency,
                MathF.BitIncrement(layerDepth),
                drawStackNumber,
                color,
                drawShadow);

            return false;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("overriding drawing for combined ring", ex);
        }

        return true;
    }
}
