using System.Reflection;
using System.Reflection.Emit;
using AtraBase.Toolkit.Reflection;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using MoreFertilizers.Framework;
using Netcode;
using StardewValley.Buildings;
using StardewValley.Objects;

namespace MoreFertilizers.HarmonyPatches.OrganicFertilizer;

/// <summary>
/// Transpiles the mill.
/// </summary>
[HarmonyPatch(typeof(Mill))]
internal static class MillDayUpdateTranspiler
{
    private static SObject? MakeOrganic(Item? input, SObject? output)
    {
        if (input is null || output is null)
        {
            return output;
        }
        try
        {
            if (input.modData?.GetBool(CanPlaceHandler.Organic) == true)
            {
                output.modData?.SetBool(CanPlaceHandler.Organic, true);
                if (!output.Name.Contains("Organic"))
                {
                    output.Name += " (Organic)";
                }
                output.MarkContextTagsDirty();
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Error in making mill item organic!\n\n{ex}", LogLevel.Error);
        }
        return output;
    }

#pragma warning disable SA1116 // Split parameters should start on line after declaration
    [HarmonyPatch(nameof(Mill.dayUpdate))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.DeclareLocal(typeof(Item), out LocalBuilder? inputlocal)
            .FindNext(new CodeInstructionWrapper[]
            { // Find the call to get_Item.
                new(OpCodes.Callvirt, typeof(NetFieldBase<Chest, NetRef<Chest>>).InstancePropertyNamed("Value").GetGetMethod()),
                new(OpCodes.Ldfld, typeof(Chest).InstanceFieldNamed(nameof(Chest.items))),
                new(SpecialCodeInstructionCases.LdLoc),
                new(OpCodes.Callvirt, typeof(NetList<Item, NetRef<Item>>).InstancePropertyNamed("Item").GetGetMethod()),
            })
            .Advance(4)
            .Insert(new CodeInstruction[]
            { // This is sufficiently annoying we're just going to create a local to store it.
                new(OpCodes.Stloc, inputlocal),
                new(OpCodes.Ldloc, inputlocal),
            })
            .FindNext(new CodeInstructionWrapper[]
            { // find and store the output's local.
                new(OpCodes.Newobj, typeof(SObject).GetConstructor(new[] { typeof(int), typeof(int), typeof(bool), typeof(int), typeof(int) })),
                new(SpecialCodeInstructionCases.StLoc),
                new(SpecialCodeInstructionCases.Wildcard, (inst) => inst.Branches(out _)),
            }).Advance(1);

            CodeInstruction? stoutput = helper.CurrentInstruction.Clone();
            CodeInstruction? ldoutput = helper.CurrentInstruction.ToLdLoc();

            // Advance to the end of the switch case.
            helper.Advance(1)
            .StoreBranchDest()
            .AdvanceToStoredLabel()
            .FindNext(new CodeInstructionWrapper[]
            { // Skip past the null check to the next block.
                new(SpecialCodeInstructionCases.LdLoc),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(Mill).InstanceFieldNamed(nameof(Mill.output))),
            })
            .GetLabels(out IList<Label>? labelsToMove, clear: true)
            .Insert(new CodeInstruction[]
            { // Place our function call here.
                new(OpCodes.Ldloc, inputlocal),
                ldoutput,
                new(OpCodes.Call, typeof(MillDayUpdateTranspiler).StaticMethodNamed(nameof(MakeOrganic))),
                stoutput,
            }, withLabels: labelsToMove);

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling Crop.harvest:\n\n{ex}", LogLevel.Error);
        }
        return null;
    }
#pragma warning restore SA1116 // Split parameters should start on line after declaration
}