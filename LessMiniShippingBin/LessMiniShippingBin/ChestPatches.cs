using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using StardewValley.Objects;

namespace LessMiniShippingBin;

/// <summary>
/// Patches against StardewValley.Objects.Chest.
/// </summary>
[HarmonyPatch(typeof(Chest))]
internal class ChestPatches
{
    /// <summary>
    /// Postfix against the chest capacity.
    /// </summary>
    /// <param name="__instance">The chest to look at.</param>
    /// <param name="__result">The requested size of the chest.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Chest.GetActualCapacity))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention")]
    public static void PostfixActualCapacity(Chest __instance, ref int __result)
    {
        try
        {
            if (__instance.specialChestType?.Value == Chest.SpecialChestTypes.MiniShippingBin)
            {
                __result = ModEntry.Config.Capacity;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed in overwriting mini shipping bin capacity\n\n{ex}", LogLevel.Error);
        }
    }
}