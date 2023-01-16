using System.Xml.Serialization;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;

using StardewValley.TerrainFeatures;

namespace GrowableBushes.Framework;

/// <summary>
/// A bush in the inventory.
/// </summary>
[XmlType("Mods_atravita_InventoryBush")]
public sealed class InventoryBush : SObject
{
    private int currentSeason = -1;
    private Rectangle sourceRect = default;

    /// <summary>
    /// The prefix used for the internal name of these bushes.
    /// </summary>
    internal const string BushPrefix = "atravita.InventoryBush.";

    /// <summary>
    /// The moddata string used to mark the bushes planted with this mod.
    /// </summary>
    internal const string BushModData = "atravita.InventoryBush.Planted";

    /// <summary>
    /// Constructor for the serializer.
    /// </summary>
    public InventoryBush() : base() {}

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryBush"/> class.
    /// </summary>
    /// <param name="whichBush">Which bush this inventory bush corresponds to.</param>
    /// <param name="initialStack">Initial stack of bushes.</param>
    public InventoryBush(int whichBush, int initialStack)
        : base(whichBush, initialStack, false, -1, 0)
    {
        this.ParentSheetIndex = Math.Clamp(whichBush, 0, BushSizesExtensions.Length - 1);

        this.bigCraftable.Value = true;
        this.Name = BushPrefix + ((BushSizes)this.ParentSheetIndex).ToStringFast();
        this.Edibility = inedible;
        this.Price = 0;
        this.Category = -15500057; // random negative integer.

        // just to make sure the bush texture is loaded.
        _ = Bush.texture.Value;
    }

    #region reflection

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

    #endregion

    #region placement

    // TODO: actually check like boundaries?
    public override bool canBePlacedHere(GameLocation l, Vector2 tile) => true;

    public override bool placementAction(GameLocation location, int x, int y, Farmer? who = null)
    {
        BushSizes size = (BushSizes)this.ParentSheetIndex;

        Vector2 placementTile = new (x / Game1.tileSize, y / Game1.tileSize);
        Bush bush = new (placementTile, size.ToStardewBush(), location);

        switch (size)
        {
            case BushSizes.AlternativeSmall:
                bush.tileSheetOffset.Value = 1;
                break;
            case BushSizes.Town:
                bush.townBush.Value = true;
                break;
            case BushSizes.Medium:
                bush.townBush.Value = false;
                break;
        }

        bush.modData.SetBool(BushModData, true);
        location.largeTerrainFeatures.Add(bush);
        location.playSound("thudStep");
        BushShakeMethod(bush, placementTile, true);
        return true;
    }

    #endregion

    // TODO: draw, draw in menu, draw while holding overhead. SObject has too many draw methods XD
    // placement bounds too!

    #region misc

    /// <inheritdoc />
    public override bool canBeShipped() => false;

    /// <inheritdoc />
    public override bool canBeGivenAsGift() => false;

    /// <inheritdoc />
    public override bool canBeTrashed() => true;

    /// <inheritdoc />
    public override string getCategoryName() => I18n.Category();

    /// <inheritdoc />
    public override Color getCategoryColor() => Color.Green;

    /// <inheritdoc />
    protected override string loadDisplayName() => (BushSizes)this.ParentSheetIndex switch
    {
        BushSizes.Small => I18n.Bush_Small(),
        BushSizes.Medium => I18n.Bush_Medium(),
        BushSizes.Large => I18n.Bush_Large(),
        BushSizes.AlternativeSmall => I18n.Bush_Small_Alt(),
        _ => I18n.Bush_Town(),
    };

    #endregion

    #region helpers

    private static int GetSeason(GameLocation loc)
        => Utility.getSeasonNumber(Game1.GetSeasonForLocation(loc));
    /*
    private Rectangle GetSourceRectForSeason(int season)
    {
        switch (this.ParentSheetIndex)
        {
            case Bush.smallBush:
                return 
            case Bush.mediumBush:
            default:
        }
    }*/

    #endregion
}
