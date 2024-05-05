using System.Reflection.Emit;

using AtraCore.Framework.ReflectionManager;

using CameraPan.Framework;

using HarmonyLib;

using StardewValley.Menus;

namespace CameraPan.HarmonyPatches;

/// <summary>
/// Holds patches to make the zoom box wider.
/// </summary>
[HarmonyPatch(typeof(DayTimeMoneyBox))]
internal static class ZoomPatches
{
    [HarmonyPatch(nameof(DayTimeMoneyBox.receiveLeftClick))]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.Is(OpCodes.Ldc_R4, 2f))
            {
                yield return new CodeInstruction(
                    OpCodes.Call,
                    typeof(ModEntry).GetCachedProperty(nameof(ModEntry.Config), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod(true)).WithLabels(instruction.labels);
                yield return new(OpCodes.Callvirt, typeof(ModConfig).GetCachedProperty(nameof(ModConfig.MaxZoom), ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod(true));
            }
            else if (instruction.Is(OpCodes.Ldc_R4, 0.75f))
            {
                yield return new CodeInstruction(
                    OpCodes.Call,
                    typeof(ModEntry).GetCachedProperty(nameof(ModEntry.Config), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod(true)).WithLabels(instruction.labels);
                yield return new(OpCodes.Callvirt, typeof(ModConfig).GetCachedProperty(nameof(ModConfig.MinZoom), ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod(true));
            }
            else
            {
                yield return instruction;
            }
        }
    }
}
