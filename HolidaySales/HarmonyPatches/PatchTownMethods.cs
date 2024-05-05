using System.Reflection;
using System.Reflection.Emit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using StardewValley.Locations;

namespace HolidaySales.HarmonyPatches;

/// <summary>
/// Holds patches that move the check for whether a festival is happening to check only for festivals happening
/// "in town", so to say.
/// </summary>
[HarmonyPatch]
internal static class PatchTownMethods
{
    /// <summary>
    /// Gets the methods to patch.
    /// </summary>
    /// <returns>An IEnumerable of methods to patch.</returns>
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        yield return typeof(Forest).GetCachedMethod("resetSharedState", ReflectionCache.FlagTypes.InstanceFlags);
        yield return typeof(IslandSouth).GetCachedMethod(nameof(IslandSouth.SetupIslandSchedules), ReflectionCache.FlagTypes.StaticFlags);
        yield return typeof(NPC).GetCachedMethod(nameof(NPC.tryToReceiveActiveObject), ReflectionCache.FlagTypes.InstanceFlags);
        yield return typeof(Farmer).GetCachedMethod(nameof(Farmer.showToolUpgradeAvailability), ReflectionCache.FlagTypes.InstanceFlags);

        if (AccessTools.TypeByName("GingerIslandMainlandAdjustments.ScheduleManager.GIScheduler") is Type gima)
        {
            yield return gima.GetCachedMethod("GenerateAllSchedules", ReflectionCache.FlagTypes.StaticFlags);
        }
        yield break;
    }

    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.AdjustIsFestivalCallForTown();

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }
}
