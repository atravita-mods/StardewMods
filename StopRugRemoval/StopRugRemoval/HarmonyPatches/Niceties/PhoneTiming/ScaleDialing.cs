using System.Reflection;
using System.Reflection.Emit;
using AtraBase.Toolkit.Extensions;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;

namespace StopRugRemoval.HarmonyPatches.Niceties.PhoneTiming;

[HarmonyPatch(typeof(GameLocation))]
internal static class ScaleDialing
{
    internal static void ApplyPatches(Harmony harmony)
    {
        Type cart = AccessTools.TypeByName("PhoneTravelingCart.Framework.Patchers.GameLocationPatcher");
        MethodInfo method = AccessTools.Method(cart, "playShopPhoneNumberSounds");

        if (method is not null)
        {
            harmony.Patch(method, transpiler: new HarmonyMethod(typeof(ScaleDialing), nameof(Transpiler)));
        }
    }

    private static int AdjustPhoneFreeze(int prevtime)
        => (int)(prevtime / ModEntry.Config.PhoneSpeedUpFactor);

    [HarmonyPatch("playShopPhoneNumberSounds")]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.ForEachMatch(
                new CodeInstructionWrapper[]
                {
                    new(SpecialCodeInstructionCases.Wildcard, (instr) => instr.opcode == OpCodes.Ldstr && ((string)instr.operand).StartsWith("telephone", StringComparison.Ordinal)),
                    new(OpCodes.Ldc_I4),
                },
                (helper) =>
                {
                    helper.Advance(2)
                    .Insert(new CodeInstruction[]
                    {
                        new(OpCodes.Call, typeof(ScalePhoneCall).GetCachedMethod(nameof(AdjustPhoneFreeze), ReflectionCache.FlagTypes.StaticFlags)),
                    });
                    return true;
                });

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling {original.GetFullName()}\n\n{ex}", LogLevel.Error);
            original?.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
}