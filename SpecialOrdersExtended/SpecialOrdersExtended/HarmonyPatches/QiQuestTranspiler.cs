using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using StardewValley.SpecialOrders;

namespace SpecialOrdersExtended.HarmonyPatches;

[HarmonyPatch(typeof(SpecialOrder))]
internal static class QiQuestTranspiler
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool DeduplicateQuests(string order_type) => order_type == "Qi" && ModEntry.Config.AvoidRepeatingQiOrders;

    [HarmonyPatch(nameof(SpecialOrder.UpdateAvailableSpecialOrders))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(
            [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldstr, string.Empty),
                new(OpCodes.Call, typeof(string).GetCachedMethod<string, string>("op_Equality", ReflectionCache.FlagTypes.StaticFlags)),
            ])
            .Advance(3)
            .Insert(
            [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(QiQuestTranspiler).GetCachedMethod(nameof(DeduplicateQuests), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Or),
            ]);

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