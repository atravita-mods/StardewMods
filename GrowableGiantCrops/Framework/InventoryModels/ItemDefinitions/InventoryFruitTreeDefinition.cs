using AtraBase.Toolkit.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.ItemTypeDefinitions;

namespace GrowableGiantCrops.Framework.InventoryModels.ItemDefinitions;

/// <inheritdoc />
public class InventoryFruitTreeDefinition : BaseItemDataDefinition
{
    /// <inheritdoc />
    public override string Identifier => "(atravitaFruitTree)";

    /// <inheritdoc />
    public override Item CreateItem(ParsedItemData data) => throw new NotImplementedException();

    /// <inheritdoc />
    public override bool Exists(string itemId) => throw new NotImplementedException();

    /// <inheritdoc />
    public override IEnumerable<string> GetAllIds() => Game1.fruitTreeData.Keys;

    /// <inheritdoc />
    public override ParsedItemData? GetData(string itemId)
    {
        if (Game1.fruitTreeData.TryGetValue(itemId, out StardewValley.GameData.FruitTrees.FruitTreeData? fruitTree))
        {
            var internalName = Game1.objectInformation.TryGetValue(itemId, out var data) ? data.GetNthChunk('/').ToString() : "UNKNOWN NAME";

            string displayName = TokenParser.ParseText(fruitTree.DisplayName);
            return new ParsedItemData(
                this,
                itemId,
                fruitTree.TextureSpriteRow,
                fruitTree.Texture,
                InventoryFruitTree.InventoryTreePrefix,
                I18n.FruitTree_Name(displayName),
                I18n.FruitTree_Description(displayName),
                InventoryFruitTree.InventoryTreeCategory,
                nameof(InventoryFruitTree),
                fruitTree);
        }

        return null;
    }

    /// <inheritdoc />
    public override Rectangle GetSourceRect(ParsedItemData data, Texture2D texture, int spriteIndex) => throw new NotImplementedException();
}
