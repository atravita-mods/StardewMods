namespace TrashDoesNotConsumeBait.HarmonyPatches;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Menus;
using StardewValley.Tools;

/// <summary>
/// Class that holds patches against the treasure menu.
/// </summary>
[HarmonyPatch(typeof(FishingRod))]
internal static class TreasureMenuPatches
{
    [HarmonyPatch(nameof(FishingRod.openTreasureMenuEndFunction))]
    private static void Postfix()
    {
        if (Game1.activeClickableMenu is not ItemGrabMenu itemGrab || itemGrab.source != ItemGrabMenu.source_fishingChest || !ModEntry.Config.EmptyFishingChests)
        {
            return;
        }

        try
        {
            for (int i = itemGrab.ItemsToGrabMenu.actualInventory.Count - 1; i >= 0; i--)
            {
                Item? item = itemGrab.ItemsToGrabMenu.actualInventory[i];
                if (item is null)
                {
                    itemGrab.ItemsToGrabMenu.actualInventory.RemoveAt(i);
                    continue;
                }

                var original = item.Stack;
                Item? remainder = item;
                if (Game1.player.CurrentTool is FishingRod rod && item is SObject obj)
                {
                    SObject? oldAttach = rod.attach(obj);
                    if (oldAttach is not null)
                    {
                        remainder = rod.attach(oldAttach);
                    }
                    else
                    {
                        remainder = null;
                    }
                }

                remainder = Game1.player.addItemToInventory(remainder);
                if (remainder is null)
                {
                    itemGrab.ItemsToGrabMenu.actualInventory.RemoveAt(i);
                }
                else
                {
                    itemGrab.ItemsToGrabMenu.actualInventory[i] = remainder;
                }

                if (remainder is null || remainder.Stack < original)
                {
                    Game1.addHUDMessage(HUDMessage.ForItemGained(item, original - (remainder?.Stack ?? 0)));
                }
            }

            if (itemGrab.areAllItemsTaken())
            {
                itemGrab.exitThisMenuNoSound();
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"moving items from treasure chest to inventory", ex);
        }
    }
}