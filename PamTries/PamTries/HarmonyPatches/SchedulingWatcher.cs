using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewModdingAPI.Utilities;

namespace PamTries.HarmonyPatches;

/// <summary>
/// Watches the scheduler to check if Pam's driving the bus today.
/// </summary>
[HarmonyPatch(typeof(NPC))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class SchedulingWatcher
{
    /// <summary>
    /// Gets a value indicating whether or not Pam drove the bus today.
    /// </summary>
    internal static bool DidPamDriveBusToday { get; private set; } = false;

    [HarmonyPatch(nameof(NPC.parseMasterSchedule))]
    private static void Postfix(NPC __instance)
    {
        try
        {
            if (Context.IsMainPlayer && __instance?.Name.Equals("Pam", StringComparison.OrdinalIgnoreCase) == true
                && __instance.TryGetScheduleEntry(__instance.dayScheduleName.Value, out string? rawData))
            {
                DidPamDriveBusToday = ModEntry.ScheduleUtilityFunctions.TryFindGOTOschedule(__instance, SDate.Now(), rawData, out string? redirected)
                    && redirected.Contains("BusStop 21 10");
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("postfixing get master schedule to get Pam's schedule", ex);
        }
    }
}
