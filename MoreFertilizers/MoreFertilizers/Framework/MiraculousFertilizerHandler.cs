using AtraBase.Toolkit.Extensions;

using AtraCore;

using AtraShared.ConstantsAndEnums;

using Microsoft.Xna.Framework;

namespace MoreFertilizers.Framework;

/// <summary>
/// Handles grabbing the right beverage for the Miraculous Fertilizer.
/// </summary>
internal static class MiraculousFertilizerHandler
{
    private static SObject? keg;

    /// <summary>
    /// Initializes the keg instance used here.
    /// Call no earlier than SaveLoaded.
    /// </summary>
    internal static void Initialize()
    {
        keg = new SObject(Vector2.Zero, "12");
    }

    /// <summary>
    /// Gets the relevant beverage for the beverage fertilizer.
    /// </summary>
    /// <param name="objindex">The index of the crop.</param>
    /// <returns>The beverage, if any.</returns>
    internal static SObject? GetBeverage(string objindex)
        => GetBeverage(new SObject(objindex, 999));

    /// <summary>
    /// Gets the relevant beverage for the beverage fertilizer.
    /// </summary>
    /// <param name="item">The crop.</param>
    /// <returns>The beverage, if any.</returns>
    internal static SObject? GetBeverage(Item item)
    {
        if (keg is null)
        {
            return null;
        }

        if (item.Stack != item.maximumStackSize())
        {
            item = item.getOne();
            item.Stack = item.maximumStackSize();
        }

        keg.heldObject.Value = null;
        keg.performObjectDropInAction(item, false, Game1.player);
        SObject? heldobj = keg.heldObject.Value;
        if (heldobj?.getOne() is SObject returnobj && Random.Shared.OfChance((25.0 + Game1.player.LuckLevel) / Math.Max(heldobj.Price, 200)))
        {
            return returnobj;
        }
        return null;
    }
}