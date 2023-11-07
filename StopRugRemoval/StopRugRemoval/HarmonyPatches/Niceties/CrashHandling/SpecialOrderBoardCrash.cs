using System.Reflection;
using System.Reflection.Emit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using Netcode;

using StardewValley.Menus;
using StardewValley.SpecialOrders;

namespace StopRugRemoval.HarmonyPatches.Niceties.CrashHandling;

/// <summary>
/// Holds patches to make special orders less fragile.
/// </summary>
[HarmonyPatch(typeof(SpecialOrder))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class SpecialOrderCrash
{
    [HarmonyPatch(nameof(SpecialOrder.GetSpecialOrder))]
    private static Exception? Finalizer(string key, ref SpecialOrder? __result, Exception? __exception)
    {
        if (__exception is not null)
        {
            ModEntry.ModMonitor.Log($"Detected invalid special order {key}.", LogLevel.Error);
            ModEntry.ModMonitor.Log(__exception.ToString());
            __result = null;
        }
        return null;
    }
}