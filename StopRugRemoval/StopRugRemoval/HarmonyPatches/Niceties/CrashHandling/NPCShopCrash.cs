using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Locations;

namespace StopRugRemoval.HarmonyPatches.Niceties.CrashHandling;

#warning - remove in 1.6

[HarmonyPatch(typeof(Game1))]
internal static class NPCShopCrash
{
    [HarmonyPatch(nameof(Game1.UpdateShopPlayerItemInventory))]
    private static void Prefix(string location_name)
    {
        if (Game1.getLocationFromName(location_name) is not ShopLocation loc || loc.itemsFromPlayerToSell.Count == 0)
        {
            return;
        }

        ModEntry.ModMonitor.DebugOnlyLog($"Checking for null items in shop list for {location_name}.");
        for (int i = loc.itemsFromPlayerToSell.Count - 1; i >= 0; i--)
        {
            Item? item = loc.itemsFromPlayerToSell[i];
            if (item is null || item.Stack <= 0)
            {
                loc.itemsFromPlayerToSell.RemoveAt(i);
            }
        }
    }
}
