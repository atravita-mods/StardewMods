// Ignore Spelling: Utils Craftable

namespace AtraShared.Utils;

using CommunityToolkit.Diagnostics;

using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Objects;

/// <summary>
/// Helper functions for items.
/// </summary>
public static class ItemHelperUtils
{
    /// <summary>
    /// Given a data string from <see cref="Game1.objectData"/>, checks to see if it's an item that shouldn't be in the player inventory.
    /// </summary>
    /// <param name="id">The ItemID of the object.</param>
    /// <param name="data">The data</param>
    /// <returns>true if it should be excluded, false otherwise.</returns>
    public static bool ObjectFilter(string id, ObjectData data)
    {
        Guard.IsNotNullOrEmpty(id);
        Guard.IsNotNull(data);

        // category asdf should never end up in the player inventory.
        if (data.Type is "asdf" or "Quest")
        {
            return true;
        }

        // block artifact spots :(
        if (id == SObject.artifactSpotID)
        {
            return true;
        }

        switch (data.Name)
        {
            case "Stone" when id != "390":
            case "Weeds":
            case "SupplyCrate":
            case "Twig":
            case "Rotten Plant":
            case "Warp Totem: Qi's Arena":
            case "???":
            case "DGA Dummy Object":
            case "Lost Book":
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Given a data string from <see cref="Game1.bigCraftableData"/>, checks to see if it's an item that shouldn't be in the player inventory.
    /// </summary>
    /// <param name="id">The ItemID of the object.</param>
    /// <param name="data">The data instance.</param>
    /// <returns>true if it should be excluded, false otherwise.</returns>
    public static bool BigCraftableFilter(string id, BigCraftableData data)
    {
        Guard.IsNotNullOrEmpty(id);
        Guard.IsNotNull(data);

        switch (data.Name)
        {
            case "Wood Chair":
            case "Door":
            case "Locked Door":
            case "Obelisk":
            case "Crate":
            case "Barrel":
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Filters the object data to just rings.
    /// </summary>
    /// <param name="id">The string id.</param>
    /// <param name="data">The object data</param>
    /// <returns>True if the item is not a ring, false otherwise.</returns>
    public static bool RingFilter(string id, ObjectData data)
    {
        Guard.IsNotNullOrEmpty(id);
        Guard.IsNotNull(data);

        // wedding ring (801) is apparently supposed to be a real ring.
        // JA rings are registered as "Basic -96"
        return id != "801" && data.Category != -96 && data.Type != "Ring";
    }
}
