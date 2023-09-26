// Ignore Spelling: Utils

using CommunityToolkit.Diagnostics;

using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Objects;

namespace AtraCore.Framework.ItemManagement;
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

        var name = data.Name;
        if (name == "Stone" && id != "390")
        {
            return true;
        }
        if (name == "Weeds"
            || name == "SupplyCrate"
            || name == "Twig"
            || name == "Rotten Plant"
            || name == "Warp Totem: Qi's Arena"
            || name == "???"
            || name == "DGA Dummy Object"
            || name == "Lost Book")
        {
            return true;
        }

        return false;
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

        string name = data.Name;
        if (name == "Wood Chair"
            || name == "Door"
            || name == "Locked Door"
            || name == "Obelisk"
            || name == "Crate"
            || name == "Barrel")
        {
            return true;
        }

        return false;
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

        // wedding ring (801) isn't a real ring.
        // JA rings are registered as "Basic -96"
        if (id == "801")
        {
            return true;
        }

        return data.Category != -96 && data.Type != "Ring";
    }
}
