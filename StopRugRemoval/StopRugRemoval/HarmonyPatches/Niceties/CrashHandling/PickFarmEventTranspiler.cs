using System.Reflection;
using System.Reflection.Emit;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;

namespace StopRugRemoval.HarmonyPatches.Niceties.CrashHandling;

/// <summary>
/// Patches against pickPersonalFarmEvent to add a null check.
/// </summary>
[HarmonyPatch(typeof(Utility))]
internal static class PickFarmEventTranspiler
{
    [HarmonyPatch(nameof(Utility.pickPersonalFarmEvent))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);

            // switch Game1.getCharacterFromName(Game1.player.spouse).canGetPregnant() to
            // Game1.getCharacterFromName(Game1.player.spouse)?.canGetPregant() == true
            Label isNull = gen.DefineLabel();
            helper.FindNext(new CodeInstructionWrapper[]
            {
                (OpCodes.Call, typeof(Game1).GetCachedMethod<string, bool, bool>(nameof(Game1.getCharacterFromName), ReflectionCache.FlagTypes.StaticFlags)),
                (OpCodes.Callvirt, typeof(NPC).GetCachedMethod(nameof(NPC.canGetPregnant), ReflectionCache.FlagTypes.InstanceFlags)),
                OpCodes.Brfalse_S,
            })
            .Advance(1)
            .DefineAndAttachLabel(out var isNotNull)
            .Push()
            .Advance(1)
            .StoreBranchDest()
            .AdvanceToStoredLabel()
            .DefineAndAttachLabel(out var jumpPoint)
            .Pop()
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Dup),
                new(OpCodes.Brfalse_S, isNull),
                new(OpCodes.Br_S, isNotNull),
                new CodeInstruction(OpCodes.Pop).WithLabels(isNull),
                new(OpCodes.Br, jumpPoint),
            });

            helper.Print();
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
