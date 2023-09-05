using AtraBase.Toolkit.Extensions;

namespace AtraCore.Framework.ItemManagement;
public static class ItemHelperUtils
{
    /// <summary>
    /// Given a data string from <see cref="Game1.objectInformation"/>, checks to see if it's an item that shouldn't be in the player inventory.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="objectData"></param>
    /// <returns></returns>
    public static bool ObjectFilter(string id, string objectData)
    {
        // category asdf should never end up in the player inventory.
        ReadOnlySpan<char> cat = objectData.GetNthChunk('/', SObject.objectInfoTypeIndex);
        if (cat.Equals("asdf", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        ReadOnlySpan<char> name = objectData.GetNthChunk('/', SObject.objectInfoNameIndex);
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
}
