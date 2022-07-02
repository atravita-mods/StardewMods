using System.Reflection;
using System.Reflection.Emit;
using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.Reflection;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;

namespace HolidaySales.HarmonyPatches;

[HarmonyPatch(typeof(GameLocation))]
internal static class RedirectPhoneCall
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (MethodInfo? method in typeof(GameLocation).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (method.Name.Contains("<answerDialogueAction>") && method.GetParameters().Length == 0)
            {
                yield return method;
            }
        }

        Type? inner = typeof(GameLocation).GetNestedType("<>c", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new MethodNotFoundException("phone");

        foreach (MethodInfo? method in inner.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (method.Name.Contains("<answerDialogueAction>") && method.GetParameters().Length == 0)
            {
                yield return method;
            }
        }

        yield break;
    }

    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);

            helper.ForEachMatch(
                new CodeInstructionWrapper[]
                {
                    new(OpCodes.Call, typeof(GameLocation).GetCachedMethod(nameof(GameLocation.AreStoresClosedForFestival), ReflectionCache.FlagTypes.StaticFlags)),
                },
                (helper) =>
                {
                    helper.ReplaceOperand(typeof(HSUtils).GetCachedMethod(nameof(HSUtils.StoresClosedForFestival), ReflectionCache.FlagTypes.StaticFlags));
                    return true;
                });

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