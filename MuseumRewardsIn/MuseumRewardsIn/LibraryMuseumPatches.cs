namespace MuseumRewardsIn;

using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewModdingAPI.Events;

using StardewValley.Locations;

/// <summary>
/// Holds patches against LibraryMuseum.
/// </summary>
[HarmonyPatch(typeof(LibraryMuseum))]
internal static class LibraryMuseumPatches
{
    /// <summary>
    /// The moddata key used to temporarily mark shop items.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Constants.")]
    internal const string MUSEUM_MARKER = "atravita.MuseumShopItem";

    /// <inheritdoc cref="IPlayerEvents.InventoryChanged"/>
    /// <param name="added">The items added.</param>
    internal static void OnInventoryChanged(IEnumerable<Item> added)
    {
        foreach (Item item in added)
        {
            if (item.modData?.GetBool(MUSEUM_MARKER) == true)
            {
                item.modData.Remove(MUSEUM_MARKER);
                item.specialItem = false;
                ModEntry.ModMonitor.DebugOnlyLog($"Removing special flag from {item.QualifiedItemId}.");
            }
        }
    }

    [HarmonyPatch(nameof(LibraryMuseum.OnRewardCollected))]
    private static void Postfix(Item item)
    {
        if (item.specialItem)
        {
            item.modData?.SetBool(MUSEUM_MARKER, true);
        }
    }
}
