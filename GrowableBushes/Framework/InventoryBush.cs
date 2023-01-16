using System.Xml.Serialization;

using AtraCore.Framework.ReflectionManager;

using Microsoft.Xna.Framework;

using StardewValley.TerrainFeatures;

namespace GrowableBushes.Framework;

/// <summary>
/// A bush in the inventory.
/// </summary>
[XmlType("Mods_atravita_InventoryBush")]
public sealed class InventoryBush : SObject
{
    /// <summary>
    /// Stardew's Bush::shake.
    /// </summary>
    private static readonly BushShakeDel BushShakeMethod = typeof(Bush)
        .GetCachedMethod("shake", ReflectionCache.FlagTypes.InstanceFlags)
        .CreateDelegate<BushShakeDel>();

    private delegate void BushShakeDel(
        Bush bush,
        Vector2 tileLocation,
        bool doEvenIfStillShaking);

    /// <summary>
    /// Constructor for the serializer.
    /// </summary>
    public InventoryBush() : base() {}

    public InventoryBush(int parentSheetIndex, int initialStack, bool isRecipe = false, int price = -1, int quality = 0)
        : base(parentSheetIndex, initialStack, isRecipe, price, quality)
    {
        this.ParentSheetIndex = Math.Clamp(parentSheetIndex, 0, 2);
        this.CanBeSetDown = true;
        this.Name = parentSheetIndex switch
        {
            Bush.smallBush => "Small Inventory Bush",
            Bush.mediumBush => "Medium Inventory Bush",
            _ => "Large Inventory Bush",
        };

        // just to make sure the bush texture is loaded.
        _ = Bush.texture.Value;
    }

    // TODO: actually check like boundaries?
    public override bool placementAction(GameLocation location, int x, int y, Farmer? who = null)
    {
        Vector2 placementTile = new (x / Game1.tileSize, y / Game1.tileSize);
        Bush bush = new (placementTile, this.ParentSheetIndex, location);
        location.largeTerrainFeatures.Add(bush);
        location.playSound("thudStep");
        BushShakeMethod(bush, placementTile, true);
        return true;
    }

    // TODO: draw, draw in menu, draw while holding overhead. SObject has too many draw methods XD
}
