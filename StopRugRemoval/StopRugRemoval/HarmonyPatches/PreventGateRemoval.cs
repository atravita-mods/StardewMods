namespace StopRugRemoval.HarmonyPatches;

using System.Reflection;
using System.Reflection.Emit;

using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using StardewModdingAPI.Utilities;

/// <summary>
/// Patches to prevent gate removal.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
internal static class PreventGateRemoval
{
    private static readonly PerScreen<int> PerscreenedAttempts = new(createNewState: () => 0);
    private static readonly PerScreen<int> PerscreenedTicks = new(createNewState: () => 0);

    private static int Attempts
    {
        get => PerscreenedAttempts.Value;
        set => PerscreenedAttempts.Value = value;
    }

    private static int Ticks
    {
        get => PerscreenedTicks.Value;
        set => PerscreenedTicks.Value = value;
    }

    /// <summary>
    /// Checks to see if the FurniturePlacementKey is held.
    /// Also shows the message if needed.
    /// </summary>
    /// <param name="fence">The possible fence object.</param>
    /// <returns>true if held down, false otherwise.</returns>
    public static bool AreFurnitureKeysHeld(SObject fence)
    {
        if (Game1.ticks > Ticks + 120)
        {
            Attempts = 0;
            Ticks = Game1.ticks;
        }
        else
        {
            Attempts++;
        }
        if (ModEntry.Config.FurniturePlacementKey.IsDown() || !ModEntry.Config.Enabled)
        {
            return true;
        }
        else
        {
            if (Attempts > 12 && fence is Fence actual && actual.isGate.Value)
            {
                Attempts -= 5;
                Game1.showRedMessage(I18n.GateRemovalMessage(ModEntry.Config.FurniturePlacementKey));
            }
            return false;
        }
    }

    /**********************
    * if (vector == who.Tile && !value.isPassable())
    *
    * to
    *
    * if (vector == who.Tile && AreFurnitureKeysHeld() && !value.isPassable())
    ********************************************/
#pragma warning disable SA1116 // Split parameters should start on line after declaration. Reviewed.
    [HarmonyPatch(nameof(GameLocation.checkAction))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                (OpCodes.Ldstr, "(T)Pickaxe"),
            })
            .Advance(1)
            .FindPrev(new CodeInstructionWrapper[]
            { // !value.IsPassable()
                SpecialCodeInstructionCases.LdLoc,
                (OpCodes.Callvirt, typeof(SObject).GetCachedMethod(nameof(SObject.isPassable), ReflectionCache.FlagTypes.InstanceFlags)),
                OpCodes.Brtrue,
            })
            .Push();

            CodeInstruction fence = helper.CurrentInstruction.Clone();
            helper.Advance(2)
            .StoreBranchDest()
            .AdvanceToStoredLabel()
            .DefineAndAttachLabel(out Label newLabel)
            .Pop()
            .GetLabels(out IList<Label> labels, clear: true)
            .Insert(new CodeInstruction[]
            {
                fence,
                new(OpCodes.Call, typeof(PreventGateRemoval).StaticMethodNamed(nameof(AreFurnitureKeysHeld))),
                new(OpCodes.Brfalse, newLabel),
            },
            withLabels: labels);
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