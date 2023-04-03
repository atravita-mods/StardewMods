using System.Reflection;
using System.Reflection.Emit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

namespace CameraPan.HarmonyPatches;

/// <summary>
/// A transpiler to snap the camera back if the player takes damage.
/// </summary>
[HarmonyPatch(typeof(Farmer))]
internal static class OnDamageTakenTranspiler
{
    private static void OnDamageTaken(Farmer player)
    {
        if (ReferenceEquals(player, Game1.player) && ModEntry.Config.ResetWhenDamageTaken)
        {
            ModEntry.ZeroOffset();
            ModEntry.MSHoldOffset = 250;
        }
    }

    [HarmonyPatch(nameof(Farmer.takeDamage))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                (OpCodes.Call, typeof(Math).GetCachedMethod<int, int>(nameof(Math.Max), ReflectionCache.FlagTypes.StaticFlags)),
                (OpCodes.Stfld, typeof(Farmer).GetCachedField(nameof(Farmer.health), ReflectionCache.FlagTypes.InstanceFlags)),
            })
            .Advance(2)
            .Insert(new CodeInstruction[]
            {
                new (OpCodes.Ldarg_0),
                new (OpCodes.Call, typeof(OnDamageTakenTranspiler).GetCachedMethod(nameof(OnDamageTaken), ReflectionCache.FlagTypes.StaticFlags)),
            });

            // helper.Print();
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
