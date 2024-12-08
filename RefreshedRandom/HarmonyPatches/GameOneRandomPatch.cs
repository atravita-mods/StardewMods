using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;

namespace RefreshedRandom.HarmonyPatches;

/// <summary>
/// Patches Game1.newDayAfterFade to hijack the random.
/// </summary>
internal static class GameOneRandomPatch
{
    internal static void ApplyPatch(Harmony harmony, IReflectionHelper reflector)
    {
        List<CodeInstruction> instructions = PatchProcessor.GetOriginalInstructions(reflector.GetMethod(typeof(Game1), "_newDayAfterFade").MethodInfo);

        foreach (CodeInstruction? instr in instructions)
        {
            if (instr.opcode == OpCodes.Newobj)
            {
                ModEntry.ModMonitor.Log((instr.operand as ConstructorInfo)?.DeclaringType.FullDescription() ?? "what", LogLevel.Alert);
                break;
            }
        }

    }
}
