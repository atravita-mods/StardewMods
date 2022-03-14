using System.Reflection;
using System.Reflection.Emit;
using AtraBase.Toolkit.Reflection;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using Microsoft.Xna.Framework;
using xTile.Dimensions;

namespace StopRugRemoval.HarmonyPatches;

[HarmonyPatch(typeof(GameLocation))]
internal static class PreventGateRemoval
{

    private static int attempts = 0;
    private static int ticks = 0;

    public static bool AreFurnitureKeysHeld(GameLocation gameLocation, Location tile)
    {
        if (Game1.ticks > ticks + 120)
        {
            attempts = 0;
            ticks = Game1.ticks;
        }
        else
        {
            attempts++;
        }
        if (ModEntry.Config.FurniturePlacementKey.IsDown() || !ModEntry.Config.Enabled)
        {
            return true;
        }
        else
        {
            Vector2 v = new(tile.X, tile.Y);
            if (attempts > 12 && gameLocation.objects.TryGetValue(v, out SObject obj) && obj is Fence fence && fence.isGate.Value)
            {
                attempts -= 5;
                Game1.showRedMessage(I18n.GateRemovalMessage(ModEntry.Config.FurniturePlacementKey));
            }
            return false;
        }
    }

    /**********************
    * if (vect.Equals(who.getTileLocation()) && !this.objects[vect].isPassable())
    *
    * to
    *
    * if (AreFurnitureKeysHeld() && vect.Equals(who.getTileLocation()) && !this.objects[vect].isPassable())
    ********************************************/
    [HarmonyPatch(nameof(GameLocation.checkAction))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
                    {
                        new(OpCodes.Ldarg_0),
                        new(OpCodes.Ldfld),
                        new(SpecialCodeInstructionCases.LdLoc),
                        new(OpCodes.Callvirt),
                        new(OpCodes.Isinst),
                        new(OpCodes.Brtrue_S),
                    })
                .FindNext(new CodeInstructionWrapper[]
                    {
                        new(OpCodes.Ldloca_S),
                        new(SpecialCodeInstructionCases.LdArg),
                        new(OpCodes.Callvirt, typeof(Character).InstanceMethodNamed(nameof(Character.getTileLocation))),
                        new(OpCodes.Call),
                    })
                .GetLabels(out IList<Label> labels, clear: true)
                .Push()
                .FindNext(new CodeInstructionWrapper[]
                    {
                        new(OpCodes.Brfalse),
                        new(SpecialCodeInstructionCases.LdArg),
                        new(OpCodes.Ldfld),
                        new(SpecialCodeInstructionCases.LdLoc),
                    })
                .StoreBranchDest()
                .AdvanceToStoredLabel()
                .DefineAndAttachLabel(out Label newLabel)
                .Pop()
                .Insert(new CodeInstruction[]
                    {
                        new(OpCodes.Ldarg_0),
                        new(OpCodes.Ldarg_1),
                        new(OpCodes.Call, typeof(PreventGateRemoval).StaticMethodNamed(nameof(PreventGateRemoval.AreFurnitureKeysHeld))),
                        new(OpCodes.Brfalse, newLabel),
                    },
                    withLabels: labels.ToArray());
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Transpiler for GameLocation{nameof(GameLocation.checkAction)} failed with error {ex}", LogLevel.Error);
        }
        return null;
    }
}
