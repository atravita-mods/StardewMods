using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using HarmonyLib;

using Microsoft.Xna.Framework;

namespace CameraPan.HarmonyPatches;

/// <summary>
/// Adjusts the viewport based on the offset vector.
/// </summary>
[HarmonyPatch(typeof(Game1))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Named for Harmony.")]
internal static class ViewportAdjustmentPostfix
{
    private static bool IsInEvent()
        => Game1.CurrentEvent is Event evt && (evt.farmer is not null && !evt.isFestival);

    [MethodImpl(TKConstants.Hot)]
    [HarmonyPatch("getViewportCenter")]
    private static void Postfix(ref Point __result)
    {
        if (Game1.viewportTarget.X == -2.14748365E+09f && !IsInEvent()
            && (Math.Abs(Game1.viewportCenter.X - ModEntry.Target.X) >= 4 || Math.Abs(Game1.viewportCenter.Y - ModEntry.Target.Y) >= 4))
        {
            __result = Game1.viewportCenter = ModEntry.Target;
        }
    }
}
