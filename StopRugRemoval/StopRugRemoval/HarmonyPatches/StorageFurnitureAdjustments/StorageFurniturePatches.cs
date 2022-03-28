using HarmonyLib;
using StardewValley.Objects;

namespace StopRugRemoval.HarmonyPatches.StorageFurnitureAdjustments;

[HarmonyPatch(typeof(StorageFurniture))]
internal static class StorageFurniturePatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(StorageFurniture.ShowMenu))]
    private static void PrefixOpen(StorageFurniture __instance)
        => __instance.ClearNulls();

    [HarmonyPrefix]
    [HarmonyPatch(nameof(StorageFurniture.ShowChestMenu))]
    private static void PrefixChestOpen(StorageFurniture __instance)
        => __instance.ClearNulls();

    [HarmonyPrefix]
    [HarmonyPatch(nameof(StorageFurniture.checkForAction))]
    private static bool PrefixCheckedAction(StorageFurniture  __instance)
    {
        ModEntry.ModMonitor.Log(__instance.heldObject.Value?.DisplayName);
        if (ModEntry.Config.FurniturePlacementKey.IsDown() && Game1.player.ActiveObject is SObject obj && __instance.heldObject.Value is null)
        {
            Game1.player.ActiveObject = null;
            __instance.heldObject.Value = obj;
            return false;
        }
        return true;
    }
}