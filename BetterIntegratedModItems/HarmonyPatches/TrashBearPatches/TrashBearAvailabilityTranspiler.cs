using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using BetterIntegratedModItems.Framework;

using HarmonyLib;

using StardewValley.Locations;
using StardewValley.Network;

namespace BetterIntegratedModItems.HarmonyPatches.TrashBearPatches;

/// <summary>
/// Changes TrashBear's availability.
/// </summary>
[HarmonyPatch(typeof(Forest))]
internal static class TrashBearAvailabilityTranspiler
{
    [MethodImpl(TKConstants.Hot)]
    private static bool IsPastDateForBear()
        => Game1.stats.DaysPlayed >= ModEntry.Config.TrashBearUnlockDays;

    [HarmonyPatch("resetSharedState")]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = "Preference.")]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            { // if Game1.year > 2;
                (OpCodes.Ldsfld, typeof(Game1).GetCachedField(nameof(Game1.year), ReflectionCache.FlagTypes.StaticFlags)),
                OpCodes.Ldc_I4_2,
                OpCodes.Ble_S,
            })
            .Advance(2);

            Label jumppoint = (Label)helper.CurrentInstruction.operand;

            helper.Advance(-2)
            .GetLabels(out IList<Label>? labelsToMove)
            .Remove(3)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Call, typeof(TrashBearAvailabilityTranspiler).GetCachedMethod(nameof(IsPastDateForBear), ReflectionCache.FlagTypes.StaticFlags)),
                new (OpCodes.Brfalse, jumppoint),
            }, withLabels: labelsToMove)
            .FindNext(new CodeInstructionWrapper[]
            { // !NetWorldState.checkAnywhereForWorldStateID("trashBearDone")
                (OpCodes.Ldstr, "trashBearDone"),
                (OpCodes.Call, typeof(NetWorldState).GetCachedMethod(nameof(NetWorldState.checkAnywhereForWorldStateID), ReflectionCache.FlagTypes.StaticFlags)),
                OpCodes.Brtrue_S,
            })
            .Push()
            .Advance(3)
            .DefineAndAttachLabel(out Label skip)
            .Pop()
            .GetLabels(out IList<Label>? labelsToMoveTwo)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Call, typeof(ModEntry).GetCachedProperty(nameof(ModEntry.Config), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod(true)),
                new(OpCodes.Callvirt, typeof(ModConfig).GetCachedProperty(nameof(ModConfig.AffectTrashBear), ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod()),
                new(OpCodes.Brtrue_S, skip),
            }, withLabels: labelsToMoveTwo)
            ;

            helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling {original.FullDescription()}:\n\n{ex}", LogLevel.Error);
            original.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
}
