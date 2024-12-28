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
                Type? type = ((ConstructorInfo)instr.operand).DeclaringType;
                ModEntry.ModMonitor.Log($"Patching {type.FullDescription()}");
                harmony.Patch(
                    AccessTools.Method(type, "MoveNext"),
                    transpiler: new HarmonyMethod(typeof(GameOneRandomPatch), nameof(Transpiler))
                    );
                break;
            }
        }
    }

    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Ldstr, "seed")
                )
            .ThrowIfInvalid("newdaysync barrier seed")
            .MatchStartBackwards(
                new CodeMatch(OpCodes.Call, typeof(Utility).GetMethod(nameof(Utility.CreateRandomSeed)))
                );

            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling {original.FullDescription()}, see log for details.", LogLevel.Error);
            ModEntry.ModMonitor.Log(ex.ToString());
        }
        return null;
    }
}

