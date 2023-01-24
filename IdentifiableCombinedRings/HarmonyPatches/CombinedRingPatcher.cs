using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Netcode;

using StardewValley.Objects;

namespace IdentifiableCombinedRings.HarmonyPatches;

[HarmonyPatch(typeof(CombinedRing))]
internal class CombinedRingPatcher
{
    /// <summary>
    /// Patches drawInMenu
    /// </summary>
    /// <param name="__instance">Combined ring to check.</param>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(CombinedRing.drawInMenu))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention")]
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

            combinedRings[0].drawInMenu(spriteBatch, location, scaleSize * .75f, transparency, layerDepth, drawStackNumber, color, drawShadow);
            combinedRings[1].drawInMenu(spriteBatch, location, scaleSize * .75f, transparency, layerDepth, drawStackNumber, color, drawShadow);

            return false;
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.Log($"Failed while overriding drawing for combined ring:\n\n{ex}", LogLevel.Error);
        }

        return true;
    }
}
