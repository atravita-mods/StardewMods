using System.Reflection;
using System.Reflection.Emit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using StardewValley.Locations;

namespace StopRugRemoval.HarmonyPatches.Niceties;
#pragma warning disable SA1116 // Split parameters should start on line after declaration. Reviewed.

/// <summary>
/// Please, for the love of god, stop trying to feed your family stuff that isn't edible, Pierre.
/// </summary>
[HarmonyPatch(typeof(ShopLocation))]
internal static class FixStoreDialogue
{
    private static bool IsObjectVaguelyEdible(SObject? obj)
        => obj is not null && obj.Edibility > 0;

    [HarmonyPatch(nameof(ShopLocation.getPurchasedItemDialogueForNPC))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);

            // Find the part where Pierre talks.
            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(SpecialCodeInstructionCases.LdLoc),
                new(OpCodes.Ldstr, "Pierre"),
                new(OpCodes.Call),
                new(OpCodes.Brtrue),
                new(OpCodes.Br),
            })
            .Advance(3)
            .StoreBranchDest()
            .Advance(1);

            Label ret = (Label)helper.CurrentInstruction.operand;

            helper.AdvanceToStoredLabel()
            .GetLabels(out IList<Label>? labelsToMove)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_1),
                new(OpCodes.Call, typeof(FixStoreDialogue).GetCachedMethod(nameof(IsObjectVaguelyEdible), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Brfalse, ret),
            }, withLabels: labelsToMove);

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }
#pragma warning restore SA1116 // Split parameters should start on line after declaration
}
