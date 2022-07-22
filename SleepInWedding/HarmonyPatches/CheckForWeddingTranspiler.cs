using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using AtraBase.Toolkit;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using StardewValley.Locations;

namespace SleepInWedding.HarmonyPatches;

/// <summary>
/// Adds an additional check for the should-a-wedding-happen? check.
/// </summary>
internal static class CheckForWeddingTranspiler
{

    [MethodImpl(TKConstants.Hot)]
    private static int GetWeddingTime() => ModEntry.Config.WeddingTime;

    [HarmonyPatch(nameof(GameLocation.checkForEvents))]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = "Reviewed.")]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldsfld, typeof(Game1).GetCachedField(nameof(Game1.weddingsToday), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Callvirt, typeof(List<int>).GetCachedProperty(nameof(List<int>.Count), ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod()),
                new(OpCodes.Ldc_I4_0),
                new(SpecialCodeInstructionCases.Wildcard, (instr) => instr.opcode == OpCodes.Ble || instr.opcode == OpCodes.Ble_S),
            })
            .GetLabels(out var labelsToMove, clear: true)
            .DefineAndAttachLabel(out var skip)
            .Push()
            .Advance(3)
            .StoreBranchDest()
            .AdvanceToStoredLabel()
            .DefineAndAttachLabel(out var bypassWedding)
            .Pop()
            .Insert(new CodeInstruction[]
            { // if (Config.WeddingTime > Game1.timeOfDay) && (Game1.currentLocation is not Town), skip wedding for now.
                new(OpCodes.Call, typeof(ModEntry).GetCachedMethod(nameof(GetWeddingTime), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Ldsfld, typeof(Game1).GetCachedField(nameof(Game1.timeOfDay), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Ble, skip),
                new(OpCodes.Ldsfld, typeof(Game1).GetCachedProperty(nameof(Game1.currentLocation), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod()),
                new(OpCodes.Isinst, typeof(Town)),
                new(OpCodes.Brfalse, bypassWedding),
            }, withLabels: labelsToMove);

            helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Ran into error transpiling {original.FullDescription()}.\n\n{ex}", LogLevel.Error);
            original?.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
}
