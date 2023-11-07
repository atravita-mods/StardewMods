using AtraCore.Framework.ItemManagement;

using AtraShared.ConstantsAndEnums;

using StardewValley.Extensions;
using StardewValley.Objects;

namespace CatGiftsRedux.Framework.Pickers;

/// <summary>
/// Picks a ring.
/// </summary>
internal static class RingPicker
{
    /// <summary>
    /// Picks a ring.
    /// </summary>
    /// <param name="random">Random instance to use.</param>
    /// <returns>A random ring.</returns>
    internal static Item? Pick(Random random)
    {
        string? id = random.Choose(AssetManager.Rings);

        return id is not null ? ItemRegistry.Create($"{ItemRegistry.type_object}{id}") as Ring : null;
    }
}
