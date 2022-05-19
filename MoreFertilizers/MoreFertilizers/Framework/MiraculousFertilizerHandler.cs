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
        keg = new SObject(Vector2.Zero, 12);
    }

    /// <summary>
    /// Gets the relevant index for the crop fertilizer.
    /// </summary>
    /// <param name="objindex">The index of the crop.</param>
    /// <returns>The beverage, if any.</returns>
    internal static SObject? GetBeverage(int objindex)
    {
        if (keg is null)
        {
            return null;
        }
        SObject crop = new(objindex, 999);
        keg.heldObject.Value = null;
        keg.performObjectDropInAction(crop, false, Game1.player);
        SObject? heldobj = keg.heldObject.Value;
        if (heldobj?.getOne() is SObject returnobj && Game1.random.NextDouble() < (15.0 + Game1.player.LuckLevel) / heldobj.Price)
        {
            return returnobj;
        }
        return null;
    }
}