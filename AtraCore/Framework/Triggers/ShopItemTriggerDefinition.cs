using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.ItemTypeDefinitions;

namespace AtraCore.Framework.Triggers;

/// <summary>
/// A fake "item" used for shop triggers.
/// </summary>
internal sealed class ShopItemTriggerDefinition : BaseItemDataDefinition
{

    /// <inheritdoc/>
    public override string Identifier { get; } = "(atravita.ShopItem)";

    /// <inheritdoc/>
    public override string StandardDescriptor { get; } = "atravita.ShopItem";

    /// <inheritdoc/>
    public override Item CreateItem(ParsedItemData data) => new ShopItemTrigger(data.ItemId);

    /// <inheritdoc/>
    public override bool Exists(string itemId) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override IEnumerable<string> GetAllIds() => throw new NotImplementedException();

    /// <inheritdoc/>
    public override ParsedItemData GetData(string itemId) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override Rectangle GetSourceRect(ParsedItemData data, Texture2D texture, int spriteIndex) => throw new NotImplementedException();
}
