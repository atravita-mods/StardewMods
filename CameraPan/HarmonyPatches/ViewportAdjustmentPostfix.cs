using HarmonyLib;

using Microsoft.Xna.Framework;

namespace CameraPan.HarmonyPatches;

/// <summary>
/// Adjusts the viewport based on the offset vector.
/// </summary>
[HarmonyPatch(typeof(Game1))]
internal static class ViewportAdjustmentPostfix
{
    private static bool IsInEvent()
        => Game1.CurrentEvent is Event evt && (evt.farmer is not null && !evt.isFestival);

    [HarmonyPatch("getViewportCenter")]
    private static void Postfix(ref Point __result)
    {
        if (Game1.viewportTarget.X == -2.14748365E+09f && !IsInEvent())
        {
            __result = Game1.viewportCenter = ModEntry.Target;
        }
    }
}
