namespace AtraShared.Utils.Extensions;

/// <summary>
/// Extensions for SObject.
/// </summary>
internal static class SObjectExtensions
{
    /// <summary>
    /// Gets whether or not an SObject is a trash item.
    /// </summary>
    /// <param name="obj">SObject to check.</param>
    /// <returns>true if it's a trash item, false otherwise.</returns>
    internal static bool IsTrashItem(this SObject obj)
        => obj is not null && !obj.bigCraftable.Value && (obj.ParentSheetIndex >= 168 && obj.ParentSheetIndex < 173);

    /// <summary>
    /// Gets the internal name of a bigcraftable.
    /// </summary>
    /// <param name="bigCraftableIndex">Bigcraftable.</param>
    /// <returns>Internal name if found.</returns>
    internal static string GetBigCraftableName(this int bigCraftableIndex)
    {
        if (Game1.bigCraftablesInformation.TryGetValue(bigCraftableIndex, out string? value))
        {
            int index = value.IndexOf('/');
            if (index >= 0)
            {
                return value[..index];
            }
        }
        return "ERROR - big craftable not found!";
    }

    /// <summary>
    /// Gets the translated name of a bigcraftable.
    /// </summary>
    /// <param name="bigCraftableIndex">Index of the bigcraftable.</param>
    /// <returns>Name of the bigcraftable.</returns>
    internal static string GetBigCraftableTranslatedName(this int bigCraftableIndex)
    {
        if (Game1.bigCraftablesInformation.TryGetValue(bigCraftableIndex, out string? value))
        {
            int index = value.LastIndexOf('/');
            if (index >= 0)
            {
                return value[(index + 1)..];
            }
        }
        return "ERROR - big craftable not found!";
    }
}