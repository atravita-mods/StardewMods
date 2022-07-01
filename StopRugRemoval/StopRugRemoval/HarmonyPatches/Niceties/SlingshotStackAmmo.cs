using HarmonyLib;
using StardewValley.Tools;

namespace StopRugRemoval.HarmonyPatches.Niceties;

/// <summary>
/// Holds patches against Slingshot.
/// </summary>
[HarmonyPatch(typeof(Slingshot))]
internal static class SlingshotStackAmmo
{
    /// <summary>
    /// Replaces the slingshot attachment logic with the logic from FishingRod.
    /// This way, adding ammo causes it to stack (instead of just a straight swap).
    /// </summary>
    /// <param name="__instance">slingshot instance.</param>
    /// <param name="o">SObject to attach.</param>
    /// <param name="__result">Any remaining object, return to inventory.</param>
    /// <returns>True to continue to the vanilla function, false otherwise.</returns>
    [HarmonyPatch(nameof(Slingshot.attach))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention.")]
    private static bool Prefix(Slingshot __instance, SObject o, ref Item? __result)
    {
        try
        {
            var prev = __instance.attachments[0];

            if (prev is not null && prev.canStackWith(o))
            {
                prev.Stack = o.addToStack(prev);
                if (prev.Stack <= 0)
                {
                    prev = null;
                }
            }

            __instance.attachments[0] = o;
            Game1.playSound("button1");
            __result = prev;
            return false;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed in overriding Slingshot.{nameof(Slingshot.attach)}\n\n{ex}", LogLevel.Error);
        }
        return true;
    }
}
