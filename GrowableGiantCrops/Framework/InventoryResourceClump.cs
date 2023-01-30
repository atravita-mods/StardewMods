using System.Xml.Serialization;

using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops.Framework;

/// <summary>
/// A giant clump in the inventory.
/// </summary>
[XmlType("Mods_atravita_InventoryResourceClump")]
public sealed class InventoryResourceClump : SObject
{
    [XmlIgnore]
    private Rectangle sourceRect = default;

    internal const string ResourcePrefix = "atravita.ResourceClump.";

    /// <summary>
    /// Numeric category ID used to identify JA/vanilla giant crops.
    /// </summary>
    internal const int ResourceClump = -15576655; // set a large random negative number

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryResourceClump"/> class.
    /// This constructor is for the serializer. Do not use it.
    /// </summary>
    public InventoryResourceClump()
        : base()
    {
        this.Edibility = inedible;
        this.Price = 0;
        this.Category = ResourceClump;
    }

    public InventoryResourceClump(ResourceClumpIndexes idx, int initialStack)
        : base((int)idx, initialStack, false, -1, 0)
    {
        if (!ResourceClumpIndexesExtensions.IsDefined(idx))
        {
            ModEntry.ModMonitor.Log($"Resource clump {idx.ToStringFast()} doesn't seem to be a valid resource clump. Setting to stump.", LogLevel.Error);
            this.ParentSheetIndex = (int)ResourceClumpIndexes.Stump;
        }

        this.CanBeSetDown = true;
        this.Name = ResourcePrefix + ((ResourceClumpIndexes)this.ParentSheetIndex).ToStringFast();
        this.Edibility = inedible;
        this.Price = 0;
        this.Category = ResourceClump;
    }

    #region reflection
    private static readonly Action<ResourceClump, float> ShakeTimerSetter = typeof(ResourceClump)
        .GetCachedField("shakeTimer", ReflectionCache.FlagTypes.InstanceFlags)
        .GetInstanceFieldSetter<ResourceClump, float>();

    internal static void ShakeResourceClump(ResourceClump clump)
    {
        ShakeTimerSetter(clump, 500f);
        clump.NeedsUpdate = true;
    }
    #endregion

    #region placement

    internal bool CanPlace(GameLocation l, Vector2 tile, bool relaxed)
    {
        if (l.resourceClumps is null || Utility.isPlacementForbiddenHere(l))
        {
            return false;
        }

        // TODO

        return true;
    }

    #endregion

    #region draw

    /// <inheritdoc />
    public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1)
    {
        float draw_layer = Math.Max(
            0f,
            ((y * 64) + 40) / 10000f) + (x * 1E-05f);
        this.draw(spriteBatch, x, y, draw_layer, alpha);
    }

    private float GetScaleSize() => 1.4f;

    #endregion

    #region misc
    public override Item getOne()
    {
        InventoryResourceClump clump = new((ResourceClumpIndexes)this.ParentSheetIndex, 1);
        clump._GetOneFrom(this);
        return clump;
    }

    /// <inheritdoc />
    public override bool canBeShipped() => false;

    /// <inheritdoc />
    public override bool canBeGivenAsGift() => false;

    /// <inheritdoc />
    public override bool canBeTrashed() => true;

    /// <inheritdoc />
    public override string getCategoryName() => I18n.ResourceClumpCategory();

    /// <inheritdoc />
    public override Color getCategoryColor() => Color.SlateGray;

    /// <inheritdoc />
    public override bool isPlaceable() => true;

    /// <inheritdoc />
    public override bool canBePlacedInWater() => false;

    /// <inheritdoc />
    public override bool canStackWith(ISalable other)
    {
        if (other is not InventoryResourceClump otherBush)
        {
            return false;
        }
        return this.ParentSheetIndex == otherBush.ParentSheetIndex;
    }

    /// <inheritdoc/>
    protected override string loadDisplayName()
        => (ResourceClumpIndexes)this.ParentSheetIndex switch
            {
                ResourceClumpIndexes.Stump => I18n.Stump_Name(),
                ResourceClumpIndexes.HollowLog => I18n.HollowLog_Name(),
                ResourceClumpIndexes.Meteorite => I18n.Meteorite_Name(),
                ResourceClumpIndexes.Boulder => I18n.Boulder_Name(),
                ResourceClumpIndexes.MineRockOne or ResourceClumpIndexes.MineRockTwo => I18n.MineRockOne_Name(),
                ResourceClumpIndexes.MineRockThree or ResourceClumpIndexes.MineRockFour => I18n.MineRockOne_Name(),
                _ => I18n.ResourceClumpInvalid_Name(),
            };

    /// <inheritdoc/>
    public override string getDescription()
        => (ResourceClumpIndexes)this.ParentSheetIndex switch
            {
                ResourceClumpIndexes.Stump => I18n.Stump_Description(),
                ResourceClumpIndexes.HollowLog => I18n.HollowLog_Description(),
                ResourceClumpIndexes.Meteorite => I18n.Meteorite_Description(),
                ResourceClumpIndexes.Boulder => I18n.Boulder_Description(),
                ResourceClumpIndexes.MineRockOne or ResourceClumpIndexes.MineRockTwo => I18n.MineRockOne_Description(),
                ResourceClumpIndexes.MineRockThree or ResourceClumpIndexes.MineRockFour => I18n.MineRockOne_Description(),
                _ => I18n.ResourceClumpInvalid_Description(),
            };

    #endregion

    #region helpers


    #endregion
}
