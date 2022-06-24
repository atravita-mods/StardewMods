using System.Reflection;
using System.Reflection.Emit;
using AtraBase.Toolkit.Reflection;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using Netcode;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches.WaterLogged;

/// <summary>
/// Transpiler to insert a check for the waterlogged fertilizer.
/// </summary>
[HarmonyPatch(typeof(HoeDirt))]
internal static class PaddyWaterCheckTranspiler
{
    private static bool HasPaddyFertilizer(HoeDirt dirt)
        => ModEntry.PaddyCropFertilizerID != -1 && dirt.fertilizer.Value == ModEntry.PaddyCropFertilizerID;

    [HarmonyPatch(nameof(HoeDirt.paddyWaterCheck))]
#pragma warning disable SA1116 // Split parameters should start on line after declaration. Reviewed.
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            Label label = helper.Generator.DefineLabel();

            helper.FindNext(new CodeInstructionWrapper[]
            { // Match and advance past the is IndoorPot check.
                new (OpCodes.Callvirt, typeof(GameLocation).InstanceMethodNamed(nameof(GameLocation.getObjectAtTile))),
                new (OpCodes.Isinst, typeof(IndoorPot)),
                new (OpCodes.Brfalse_S),
            })
            .Advance(2)
            .StoreBranchDest()
            .AdvanceToStoredLabel()
            .GetLabels(out IList<Label> labels, clear: true)
            .AttachLabel(label)
            .Insert(new CodeInstruction[]
            { // Insert a check for the Waterlogged fertilizer to set the field and return true.
                new (OpCodes.Ldarg_0),
                new (OpCodes.Call, typeof(PaddyWaterCheckTranspiler).StaticMethodNamed(nameof(PaddyWaterCheckTranspiler.HasPaddyFertilizer))),
                new (OpCodes.Brfalse, label),
                new (OpCodes.Ldarg_0),
                new (OpCodes.Ldfld, typeof(HoeDirt).InstanceFieldNamed(nameof(HoeDirt.nearWaterForPaddy))),
                new (OpCodes.Ldc_I4_1),
                new (OpCodes.Callvirt, typeof(NetFieldBase<int, NetInt>).InstancePropertyNamed("Value").GetSetMethod()),
                new (OpCodes.Ldc_I4_1),
                new (OpCodes.Ret),
            }, withLabels: labels);
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling Hoedirt.Draw:\n\n{ex}", LogLevel.Error);
        }
        return null;
    }
#pragma warning restore SA1116 // Split parameters should start on line after declaration
}