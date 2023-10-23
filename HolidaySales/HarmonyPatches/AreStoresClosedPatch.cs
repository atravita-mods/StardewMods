using System.Reflection;
using System.Reflection.Emit;
using AtraBase.Toolkit.Extensions;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;

namespace HolidaySales.HarmonyPatches;

/// <summary>
/// Patch to adjust whether stores should be closed for festivals.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
internal static class AreStoresClosedPatch
{
    [HarmonyPatch(nameof(GameLocation.AreStoresClosedForFestival))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.AdjustIsFestivalCall();

            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }
}
