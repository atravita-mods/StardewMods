namespace GrowableBushes.Framework.Items;

using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley;
using StardewValley.ItemTypeDefinitions;

/// <summary>
/// Manages the data for the <see cref="InventoryBush"/> data type.
/// </summary>
public sealed class InventoryBushItemDefinition : BaseItemDataDefinition
{
    /// <inheritdoc />
    public override string Identifier { get; } = "(atravita.InventoryBush)";

    /// <inheritdoc />
    public override string StandardDescriptor { get; } = "atravita.InventoryBush";

    /// <inheritdoc />
    public override Item CreateItem(ParsedItemData data) => throw new NotImplementedException();

    /// <inheritdoc />
    public override bool Exists(string itemId) => BushSizesExtensions.TryParse(itemId, out BushSizes size) && size != BushSizes.Invalid;

    /// <inheritdoc />
    public override IEnumerable<string> GetAllIds() => BushSizesExtraExtensions.GetValid();

    /// <inheritdoc />
    public override ParsedItemData? GetData(string itemId)
    {
        if (!BushSizesExtensions.TryParse(itemId, out BushSizes size))
        {
            return null;
        }

        return new ParsedItemData(
            itemType: this,
            itemId,
            spriteIndex: (int)size,
            textureName: "TileSheets\\bushes",
            internalName: "blah",
            displayName: "blah",
            description: "blah",
            category: 3423423,
            objectType: "blah",
            rawData: null);
    }

    /// <inheritdoc />
    public override Rectangle GetSourceRect(ParsedItemData data, Texture2D texture, int spriteIndex) => throw new NotImplementedException();
}
