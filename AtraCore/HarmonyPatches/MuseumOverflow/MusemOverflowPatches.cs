using System.Runtime.InteropServices;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Locations;

namespace AtraCore.HarmonyPatches.MuseumOverflow;

/// <summary>
/// Holds patches to make museum overflow work.
/// </summary>
[HarmonyPatch]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class MusemOverflowPatches
{
    /// <summary>
    /// The key for the museum inventory.
    /// </summary>
    private const string INVENTORY_NAME = "atravita.AtraCore.MuseumOverflow";

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LibraryMuseum), nameof(LibraryMuseum.GetDonatedByContextTag))]
    private static void PostfixMuseumItems(Dictionary<string, int> __result)
    {
        try
        {
            var storage = Game1.player.team.GetOrCreateGlobalInventory(INVENTORY_NAME);
            storage.RemoveEmptySlots();

            if (storage.Count == 0)
            {
                return;
            }

            ref int prev = ref CollectionsMarshal.GetValueRefOrAddDefault(__result, string.Empty, out _);
            prev += storage.Count;

            foreach (Item? item in storage)
            {
                foreach (string? tag in ItemContextTagManager.GetBaseContextTags(item.ItemId))
                {
                    if (__result.TryGetValue(tag, out var value))
                    {
                        __result[tag] = value + 1;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding storage items to museum", ex);
        }
    }
}
