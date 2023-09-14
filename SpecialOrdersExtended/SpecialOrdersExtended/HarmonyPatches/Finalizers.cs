using AtraShared.ConstantsAndEnums;

using HarmonyLib;

using StardewValley.SpecialOrders;

namespace SpecialOrdersExtended.HarmonyPatches;

/// <summary>
/// Holds the finalizers for this project.
/// </summary>
[HarmonyPatch(typeof(SpecialOrder))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal class Finalizers
{
    /// <summary>
    /// Finalizes GetSpecialOrder to return null of there's an error.
    /// </summary>
    /// <param name="key">Key of the special order.</param>
    /// <param name="__result">The parsed special order, set to null to remove.</param>
    /// <param name="__exception">The observed exception.</param>
    /// <returns>null to suppress the error.</returns>
    [HarmonyFinalizer]
    [HarmonyPatch(nameof(SpecialOrder.GetSpecialOrder))]
    public static Exception? FinalizeGetSpecialOrder(string key, ref SpecialOrder? __result, Exception? __exception)
    {
        if (__exception is not null)
        {
            ModEntry.ModMonitor.Log($"Detected invalid special order {key}:", LogLevel.Error);
            ModEntry.ModMonitor.Log(__exception.ToString());
            __result = null;
        }
        return null;
    }
}