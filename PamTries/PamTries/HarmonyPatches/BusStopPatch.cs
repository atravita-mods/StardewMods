using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Locations;

namespace PamTries.HarmonyPatches;

/// <summary>
/// Patches for bus stops.
/// </summary>
[HarmonyPatch(typeof(BusStop))]
internal static class BusStopPatch
{
    [MethodImpl(TKConstants.Hot)]
    private static bool ShouldAllowBus(GameLocation loc)
    {
        Vector2 bustile = new(11f, 10f);
        foreach(NPC npc in loc.characters)
        {
            if (npc.IsVillager && npc.Tile.Equals(bustile))
            {
                ModEntry.ModMonitor.DebugOnlyLog($"Subbing in {npc.Name} as the bus driver.", LogLevel.Info);
                return true;
            }
        }
        return false;
    }

    [HarmonyPatch(nameof(BusStop.answerDialogue), new Type[] { typeof(Response) })]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        /*******************************
         * Want to: remove the check for Pam from
         * if (Game1.player.Money >= (Game1.shippingTax ? 50 : 500) && base.characters.Contains(characterFromName) && characterFromName.getTileLocation().Equals(new Vector2(11f, 10f)))
         * AND replace it with our own check.
         *
         * TODO: figure out what draws Pam in the bus and remove that too...
        *****************************************/

        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(
                [
                    new(OpCodes.Ldstr, "Pam"),
                    new(OpCodes.Ldc_I4_1),
                    new(OpCodes.Ldc_I4_0),
                    new(OpCodes.Call),
                ])
                .FindNext(
                [
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld),
                    new(SpecialCodeInstructionCases.LdLoc),
                    new(OpCodes.Callvirt),
                    new(OpCodes.Brfalse),
                ])
                .RemoveIncluding(
                [
                    new (OpCodes.Ldc_R4, 11),
                    new (OpCodes.Ldc_R4, 10),
                    new (OpCodes.Newobj),
                    new (OpCodes.Call, typeof(Vector2).GetCachedMethod(nameof(Vector2.Equals), ReflectionCache.FlagTypes.InstanceFlags, [typeof(Vector2)])),
                ])
                .Insert(
                [
                    new (OpCodes.Ldarg_0),
                    new (OpCodes.Call, typeof(BusStopPatch).GetCachedMethod(nameof(ShouldAllowBus), ReflectionCache.FlagTypes.StaticFlags)),
                ]);
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
            return null;
        }
    }
}