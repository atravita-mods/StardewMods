using HarmonyLib;

using StardewValley.ItemTypeDefinitions;

namespace StopRugRemoval.HarmonyPatches;

[HarmonyPatch(typeof(Farmer))]
internal static class FarmerPatches
{
    [HarmonyPatch(nameof(Farmer.addItemToInventory), new Type[] { typeof(Item), typeof(List<Item>) })]
    private static void Postfix(Farmer __instance, Item item, ref Item? __result)
    {
        if (__result is not SObject obj)
        {
            return;
        }

        // try to add items to slots in the farmer's tools.
        SObject? remainder = obj;
        foreach (Item? i in __instance.Items)
        {
            if (i is Tool tool && tool.AttachmentSlotsCount > 0)
            {
                int original_stack = remainder.Stack;
                SObject? prev = tool.attach(remainder);
                if (prev is not null)
                {
                    remainder = tool.attach(prev);
                }
                else
                {
                    remainder = null;
                }

                int addedNumber = original_stack - (remainder?.Stack ?? 0);
                if (addedNumber > 0)
                {
                    ModEntry.ModMonitor.Log($"Adding {addedNumber} {obj.QualifiedItemId} to {tool.QualifiedItemId}");
                    Game1.player.OnItemReceived(item, addedNumber, prev, false);
                }

                if (remainder is null)
                {
                    break;
                }
            }
        }

        __result = remainder;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Farmer.couldInventoryAcceptThisItem), new Type[] {typeof(Item)})]
    private static void PostfixInventoryAcceptance(Farmer __instance, Item item, ref bool __result)
    {
        if (__result)
        {
            return;
        }

        foreach (Item? i in __instance.Items)
        {
            if (i is Tool t && t.AttachmentSlotsCount > 0)
            {
                foreach (SObject? slot in t.attachments)
                {
                    if (slot is not null && slot.canStackWith(item))
                    {
                        __result = true;
                        return;
                    }
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Farmer.couldInventoryAcceptThisItem), new Type[] { typeof(string), typeof(int), typeof(int) })]
    private static void PostfixInventoryAcceptance(Farmer __instance, string id, int stack, int quality, ref bool __result)
    {
        if (__result)
        {
            return;
        }
        ParsedItemData data = ItemRegistry.GetDataOrErrorItem(id);

        foreach (Item? i in __instance.Items)
        {
            if (i is Tool t && t.AttachmentSlotsCount > 0)
            {
                foreach (SObject? slot in t.attachments)
                {
                    if (slot is not null && data.QualifiedItemId == slot.QualifiedItemId && slot.Quality == quality && (slot.Stack + stack) <= slot.maximumStackSize())
                    {
                        __result = true;
                        return;
                    }
                }
            }
        }
    }
}
