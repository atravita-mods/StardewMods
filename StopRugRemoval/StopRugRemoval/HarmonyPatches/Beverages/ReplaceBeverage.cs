#if DEBUG

using System.Reflection;
using System.Reflection.Emit;

using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.Reflection;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using AtraShared.Wrappers;

using HarmonyLib;

using StardewValley.Extensions;
using StardewValley.Locations;

namespace StopRugRemoval.HarmonyPatches.Beverages;

/// <summary>
/// Replaces the coffee at the night market with a random beverage.
/// </summary>
[HarmonyPatch(typeof(BeachNightMarket))]
internal static class ReplaceBeverage
{
    private static readonly Lazy<List<string>> LazyBeverages = new(GetBeverageIDs);

    /// <summary>
    /// Gets the item ID of a random beverage.
    /// </summary>
    /// <returns>int ID of beverage.</returns>
    public static string GetRandomBeverageId() => Random.Shared.ChooseFrom(LazyBeverages.Value);

    [HarmonyPatch(nameof(BeachNightMarket.getFreeGift))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        requiresRework

        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
                    {
                        new(OpCodes.Ldc_I4, 395),
                        new(OpCodes.Ldc_I4_1),
                        new(OpCodes.Ldc_I4_0),
                        new(OpCodes.Ldc_I4_M1),
                        new(OpCodes.Ldc_I4_0),
                    })
                .ReplaceInstruction(new(OpCodes.Call, typeof(ReplaceBeverage).StaticMethodNamed(nameof(GetRandomBeverageId))), keepLabels: true);
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }

    private static List<string> GetBeverageIDs()
    {
        List<string> beverageIds = new();
        foreach ((string key, string value) in Game1Wrappers.ObjectData)
        {
            if (value.GetNthChunk('/', 6).Contains("drink", StringComparison.OrdinalIgnoreCase))
            {
                beverageIds.Add(key);
            }
        }
        return beverageIds;
    }
}
#endif