using AtraBase.Toolkit.Extensions;

using CommunityToolkit.Diagnostics;

namespace AtraCore.Framework.ItemManagement;
public static class ItemHelperUtils
{
    /// <summary>
    /// Given a data string from <see cref="Game1.objectInformation"/>, checks to see if it's an item that shouldn't be in the player inventory.
    /// </summary>
    /// <param name="id">The ItemID of the object.</param>
    /// <param name="data">The data string.</param>
    /// <returns>true if it should be excluded, false otherwise.</returns>
    public static bool ObjectFilter(string id, string data)
    {
        Guard.IsNotNullOrEmpty(id);
        Guard.IsNotNullOrEmpty(data);

        // category asdf should never end up in the player inventory.
        ReadOnlySpan<char> cat = data.GetNthChunk('/', SObject.objectInfoTypeIndex);
        if (cat.Equals("asdf", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        ReadOnlySpan<char> name = data.GetNthChunk('/', SObject.objectInfoNameIndex);
        if (name.Equals("Stone", StringComparison.OrdinalIgnoreCase) && id != "390")
        {
            return true;
        }
        if (name.Equals("Weeds", StringComparison.OrdinalIgnoreCase)
            || name.Equals("SupplyCrate", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Twig", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Rotten Plant", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Warp Totem: Qi's Arena", StringComparison.OrdinalIgnoreCase)
            || name.Equals("???", StringComparison.OrdinalIgnoreCase)
            || name.Equals("DGA Dummy Object", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Lost Book", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Given a data string from <see cref="Game1.bigCraftablesInformation"/>, checks to see if it's an item that shouldn't be in the player inventory.
    /// </summary>
    /// <param name="id">The ItemID of the object.</param>
    /// <param name="objectData">The data string.</param>
    /// <returns>true if it should be excluded, false otherwise.</returns>
    public static bool BigCraftableFilter(string id, string data)
    {
        Guard.IsNotNullOrEmpty(id);
        Guard.IsNotNullOrEmpty(data);

        ReadOnlySpan<char> nameSpan = data.GetNthChunk('/', SObject.objectInfoNameIndex);
        if (nameSpan.Equals("Wood Chair", StringComparison.OrdinalIgnoreCase)
            || nameSpan.Equals("Door", StringComparison.OrdinalIgnoreCase)
            || nameSpan.Equals("Locked Door", StringComparison.OrdinalIgnoreCase)
            || nameSpan.Equals("Obelisk", StringComparison.OrdinalIgnoreCase)
            || nameSpan.Equals("Crate", StringComparison.OrdinalIgnoreCase)
            || nameSpan.Equals("Barrel", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
