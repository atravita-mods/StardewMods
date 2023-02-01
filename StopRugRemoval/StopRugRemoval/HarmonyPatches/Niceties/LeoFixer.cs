using System.Reflection;
using System.Reflection.Emit;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

namespace StopRugRemoval.HarmonyPatches.Niceties;

/// <summary>
/// Makes it so Leo doesn't show up at the winter star festival unless he's moved to the island.
/// </summary>
[HarmonyPatch(typeof(Utility))]
internal static class LeoFixer
{
    [HarmonyPatch(nameof(Utility.getRandomTownNPC), new[] { typeof(Random) })]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                (OpCodes.Ldstr, "addedParrotBoy"),
            })
            .ReplaceOperand("leoMoved");

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Ran into error transpiling {original.FullDescription()}.\n\n{ex}", LogLevel.Error);
            original.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
}
