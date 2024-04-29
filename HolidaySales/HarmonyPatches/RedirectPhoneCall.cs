namespace HolidaySales.HarmonyPatches;

using System.Reflection;
using System.Reflection.Emit;

using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using StardewValley.Objects;

/// <summary>
/// Patch to handle the phone.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
internal static class RedirectPhoneCall
{
    /// <summary>
    /// Gets the methods to patch.
    /// </summary>
    /// <returns>methods to patch.</returns>
    /// <exception cref="MethodNotFoundException">Method wasn't found.</exception>
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (MethodInfo? method in typeof(DefaultPhoneHandler).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (method.GetParameters().Length == 0
                && method.Name.Contains("<Call", StringComparison.Ordinal)
                && ShouldTranspileThisMethod(method))
            {
                yield return method;
            }
        }

        Type[] inners = typeof(DefaultPhoneHandler).GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (Type inner in inners)
        {
            if (!inner.Name.StartsWith("<>c", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (MethodInfo? method in inner.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (method.GetParameters().Length == 0
                    && method.Name.Contains("<Call", StringComparison.Ordinal)
                    && ShouldTranspileThisMethod(method))
                {
                    yield return method;
                }
            }
        }
    }

    private static bool ShouldTranspileThisMethod(MethodInfo method)
        => PatchProcessor.GetOriginalInstructions(method)
        .Any(static (instr) => instr.Calls(typeof(GameLocation).GetCachedMethod(nameof(GameLocation.AreStoresClosedForFestival), ReflectionCache.FlagTypes.StaticFlags)));

    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);

            helper.ForEachMatch(
                [
                    (OpCodes.Call, typeof(GameLocation).GetCachedMethod(nameof(GameLocation.AreStoresClosedForFestival), ReflectionCache.FlagTypes.StaticFlags)),
                ],
                (helper) =>
                {
                    helper.ReplaceOperand(typeof(HSUtils).GetCachedMethod(nameof(HSUtils.StoresClosedForFestival), ReflectionCache.FlagTypes.StaticFlags));
                    return true;
                });

            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }
}