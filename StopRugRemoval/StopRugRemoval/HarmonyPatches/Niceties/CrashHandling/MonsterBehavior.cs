using System.Reflection;
using System.Reflection.Emit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using StardewValley.Monsters;

namespace StopRugRemoval.HarmonyPatches.Niceties.CrashHandling;

[HarmonyPatch]
internal static class MonsterBehavior
{
    [HarmonyPatch(typeof(Monster), nameof(Monster.behaviorAtGameTick))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);

            helper.FindNext(
            [
                OpCodes.Ldarg_0,
                (OpCodes.Call, typeof(Monster).GetCachedProperty(nameof(Monster.Player), ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod()),
                (OpCodes.Ldfld, typeof(Farmer).GetCachedField(nameof(Farmer.isRafting), ReflectionCache.FlagTypes.InstanceFlags)),
                OpCodes.Brfalse,
            ])
            .Push()
            .Advance(3)
            .StoreBranchDest()
            .AdvanceToStoredLabel()
            .DefineAndAttachLabel(out Label retPoint)
            .Pop()
            .Advance(2)
            .DefineAndAttachLabel(out Label isNotNull)
            .Insert(
            [
                new(OpCodes.Dup),
                new(OpCodes.Brtrue_S, isNotNull),
                new(OpCodes.Pop),
                new(OpCodes.Br, retPoint),
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
