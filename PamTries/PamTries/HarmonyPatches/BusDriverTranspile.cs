#if DEBUG

using System.Reflection;
using System.Reflection.Emit;
using AtraBase.Toolkit.Reflection;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;

namespace PamTries.HarmonyPatches;

internal static class BusDriverTranspile
{
    internal static void ApplyPatch(Harmony harmony)
    {
        try
        {
            harmony.Patch(
            original: typeof(GameLocation).InstanceMethodNamed(nameof(GameLocation.UpdateWhenCurrentLocation)),
            transpiler: new HarmonyMethod(typeof(BusDriverTranspile).StaticMethodNamed(nameof(BusDriverTranspile.Transpiler))));
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("transpiling GameLocation::UpdateWhenCurrentLocation to replace bus driver", ex);
        }
    }

    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindLast(new CodeInstructionWrapper[]
                {
                    new (OpCodes.Ldarg_0),
                    new (OpCodes.Ldstr, "Pam"),
                    new (OpCodes.Call, typeof(GameLocation).InstanceMethodNamed(nameof(GameLocation.getCharacterFromName))),
                    new (SpecialCodeInstructionCases.StLoc),
                })
                .Advance(1)
                .ReplaceInstruction(OpCodes.Call, typeof(BusDriverSchedulePatch).StaticMethodNamed(nameof(BusDriverSchedulePatch.GetCurrentDriver)), keepLabels: true);
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }
}

#endif