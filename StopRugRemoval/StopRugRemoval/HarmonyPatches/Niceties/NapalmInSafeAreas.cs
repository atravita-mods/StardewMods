using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Objects;

using StopRugRemoval.Configuration;

namespace StopRugRemoval.HarmonyPatches.Niceties;

/// <summary>
/// Holds patches to defang napalm rings in safe areas.
/// </summary>
[HarmonyPatch(typeof(Ring))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class NapalmInSafeAreas
{
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(nameof(Ring.onMonsterSlay))]
    private static bool Prefix(Ring __instance, GameLocation location)
    {
        if (ModEntry.Config.NapalmInSafeAreas)
        {
            return true;
        }

        try
        {
            if (__instance.ParentSheetIndex == 811 && location.IsLocationConsideredSafe())
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("prevent napalm ring in safe areas", ex);
        }
        return true;
    }

    private static bool IsLocationConsideredSafe(this GameLocation location)
        => ModEntry.Config.SafeLocationMap.TryGetValue(location.Name, out IsSafeLocationEnum val)
            & val == IsSafeLocationEnum.Safe;
}
