namespace AtraCore.HarmonyPatches.CustomRingPatches;

using System.Runtime.CompilerServices;

using AtraCore.Framework.Models;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Objects;

/// <summary>
/// Holds patches against the <see cref="Ring"/> class for custom rings.
/// </summary>
[HarmonyPatch(typeof(Ring))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class RingPatcher
{
    // maps the ring ID to the current effect of the ring for tooltips
    private static readonly Dictionary<string, RingEffects> _tooltipMap = new();

    // maps the rings to their active effects
    private static readonly ConditionalWeakTable<Ring, RingEffects> _activeEffects = new();

    internal static void Reset()
    {
        ModEntry.ModMonitor.DebugOnlyLog($"Resetting ring map");
        _tooltipMap.Clear();
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Ring.CanCombine))]
    private static void PostfixCanCombine(Ring __instance, Ring ring, ref bool __result)
    {
        if (!__result)
        {
            return;
        }
        if (AssetManager.GetRingData(ring.ItemId)?.CanBeCombined == false || AssetManager.GetRingData(__instance.ItemId)?.CanBeCombined == false)
        {
            __result = false;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Ring.onNewLocation))]
    private static void OnNewLocation()
    {
        ModEntry.ModMonitor.DebugOnlyLog($"Entered new location");
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Ring.AddEquipmentEffects))]
    private static void OnAddEffects()
    {
        ModEntry.ModMonitor.DebugOnlyLog($"Added Effects.");
    }
}
