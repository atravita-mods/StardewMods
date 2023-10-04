namespace CritterRings.HarmonyPatches.FrogRing;

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using Microsoft.Xna.Framework;

/// <summary>
/// Skip touch actions if the player is jumping.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
internal static class JumpTouchAction
{
    [MethodImpl(TKConstants.Hot)]
    private static bool IsActiveJump() => ModEntry.CurrentJumper?.IsValid(out Farmer? _) == true;

    [HarmonyPatch(nameof(GameLocation.UpdateWhenCurrentLocation))]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = StyleCopConstants.SplitParametersIntentional)]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            { // if this.lastTouchActionLocation.Equals(Vector2.Zero)
                OpCodes.Ldarg_0,
                (OpCodes.Ldflda, typeof(GameLocation).GetCachedField(nameof(GameLocation.lastTouchActionLocation), ReflectionCache.FlagTypes.InstanceFlags)),
                (OpCodes.Call, typeof(Vector2).GetCachedProperty(nameof(Vector2.Zero), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod()),
                OpCodes.Call,
                OpCodes.Brfalse_S,
            })
            .Push()
            .Advance(4)
            .StoreBranchDest()
            .AdvanceToStoredLabel()
            .DefineAndAttachLabel(out Label jumpPast)
            .Pop()
            .GetLabels(out IList<Label>? labelsToMove)
            .Insert(new CodeInstruction[]
            { // insert if (!IsActiveJump() && this.lastTouchActionLocation.Equals(Vector2.Zero))
                new(OpCodes.Call, typeof(JumpTouchAction).GetCachedMethod(nameof(IsActiveJump), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Brtrue, jumpPast),
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
}
