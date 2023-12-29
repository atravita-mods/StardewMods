using System.Reflection;
using System.Reflection.Emit;

using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

namespace PamTries.HarmonyPatches;

/// <summary>
/// Patches on events to hide Pam while she's at rehab.
/// </summary>
[HarmonyPatch(typeof(Event))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class EventPatches
{
    private static bool ShouldHidePam() => Game1.player.activeDialogueEvents.ContainsKey("PamTriesRehab");

    [HarmonyPostfix]
    [HarmonyPatch("addActor")]
    private static void PostfixSetup(Event __instance)
    {
        try
        {
            if (ShouldHidePam() && __instance.actors.Count > 0 && __instance.actors[^1].Name == "Pam")
            {
                __instance.actors.RemoveAt(__instance.actors.Count - 1);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("making Pam invisible", ex);
        }
    }

    // transpile the festival update method to account for the fact that I've removed Pam from the festival, lol.
    [HarmonyPatch(nameof(Event.festivalUpdate))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(
            [
                new(OpCodes.Ldstr, "iceFishing"),
            ])
            .FindNext(
            [ // this.getActorByName("Pam");
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldstr, "Pam"),
            ])
            .Push()
            .FindNext(
            [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldstr, "Elliott"),
            ])
            .DefineAndAttachLabel(out Label jumppoint)
            .Pop()
            .GetLabels(out IList<Label>? labelsToMove)
            .Insert(
            [
                new(OpCodes.Call, typeof(EventPatches).StaticMethodNamed(nameof(ShouldHidePam))),
                new(OpCodes.Brtrue, jumppoint),
            ], withLabels: labelsToMove)
            .FindNext(
            [ // the other pam block lol
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(Event).GetCachedField(nameof(Event.festivalTimer), ReflectionCache.FlagTypes.InstanceFlags)),
                new(OpCodes.Ldc_I4, 45900),
            ])
            .FindNext(
            [
                new(OpCodes.Bge),
            ])
            .StoreBranchDest()
            .Push()
            .AdvanceToStoredLabel()
            .DefineAndAttachLabel(out Label jumppass)
            .Pop()
            .Advance(1)
            .Insert(
            [
                new(OpCodes.Call, typeof(EventPatches).StaticMethodNamed(nameof(ShouldHidePam))),
                new(OpCodes.Brtrue, jumppass),
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
