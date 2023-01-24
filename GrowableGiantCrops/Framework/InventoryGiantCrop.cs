using System.Xml.Serialization;

using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Wrappers;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Netcode;

using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops.Framework;

/// <summary>
/// A class that represents a giant crop in the inventory.
/// </summary>
[XmlType("Mods_atravita_InventoryGiantCrop")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Keeping like methods together.")]
public sealed class InventoryGiantCrop : SObject
{
    #region consts

    /// <summary>
    /// A prefix used on the name of a giant crop in the inventory.
    /// </summary>
    internal const string InventoryGiantCropPrefix = "atravita.GrowableGiantCrop/";

    /// <summary>
    /// Khloe's mod data key, used to identify her giant crops.
    /// </summary>
    internal const string GiantCropTweaksModDataKey = "leclair.giantcroptweaks/Id";

    /// <summary>
    /// Numeric category ID used to identify Khloe's giant crops.
    /// </summary>
    internal const int GiantCropTweaksCategory = -13376523;

    /// <summary>
    /// Numeric category ID used to identify JA/vanilla giant crops.
    /// </summary>
    internal const int GiantCropCategory = -15577335; // set a large random negative number

    #endregion

    /// <summary>
    /// The string id, used to distinguish GiantCropTweaks giant crops.
    /// </summary>
    public readonly NetString stringID = new(string.Empty);

    #region drawfields

    [XmlIgnore]
    private Texture2D? texture;

    [XmlIgnore]
    private Rectangle sourceRect = default;

    [XmlIgnore]
    private string? texturePath;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryGiantCrop"/> class.
    /// Used for the serializer, do not use.
    /// </summary>
    public InventoryGiantCrop()
        : base()
    {
        this.NetFields.AddFields(this.stringID);
        this.Category = GiantCropCategory;
        this.Price = 0;
        this.CanBeSetDown = true;
        this.Edibility = inedible;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryGiantCrop"/> class for a GiantCropTweaks giant crop.
    /// </summary>
    /// <param name="stringID">the string id of the giantcroptweaks giant crop.</param>
    /// <param name="intID">int id of crop product.</param>
    /// <param name="initialStack">initial stack size.</param>
    public InventoryGiantCrop(string stringID, int intID, int initialStack)
        : this(intID, initialStack)
    {
        this.stringID.Value = stringID;
        this.Category = GiantCropTweaksCategory;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryGiantCrop"/> class for vanilla/JA giant crops.
    /// </summary>
    /// <param name="intID">Integer ID to use.</param>
    /// <param name="initialStack">The initial size of the stack.</param>
    public InventoryGiantCrop(int intID, int initialStack)
        : this()
    {
        this.ParentSheetIndex = intID;
        if (Game1Wrappers.ObjectInfo.TryGetValue(intID, out string? data))
        {
            this.Name = InventoryGiantCropPrefix + data.GetNthChunk('/').ToString();
        }

        // populate metadata:
        this.Stack = initialStack;
    }

    #region reflection

    /// <summary>
    /// A setter to shake a giant crop.
    /// </summary>
    private static readonly Action<GiantCrop, float> GiantCropSetShake = typeof(GiantCrop)
        .GetCachedField("shakeTimer", ReflectionCache.FlagTypes.InstanceFlags)
        .GetInstanceFieldSetter<GiantCrop, float>();

    /// <summary>
    /// A method to shake a giant crop.
    /// </summary>
    /// <param name="crop">The giant crop to shake.</param>
    internal static void ShakeGiantCrop(GiantCrop crop)
    {
        GiantCropSetShake(crop, 100f);
        crop.NeedsUpdate = true;
    }
    #endregion

    #region draw

    private void PopulateTexture()
    {

    }

    #endregion

    #region misc

    /// <inheritdoc />
    public override Item getOne()
    {
        InventoryGiantCrop crop = new(this.stringID.Value, this.ParentSheetIndex, 1);
        crop._GetOneFrom(this);
        return crop;
    }

    /// <inheritdoc />
    public override bool canBeShipped() => false;

    /// <inheritdoc />
    public override bool canBeGivenAsGift() => false;

    /// <inheritdoc />
    public override bool canBeTrashed() => true;

    /// <inheritdoc />
    public override string getCategoryName() => I18n.Category();

    /// <inheritdoc />
    public override Color getCategoryColor() => Color.ForestGreen;

    /// <inheritdoc />
    public override bool isPlaceable() => true;

    /// <inheritdoc />
    public override bool canBePlacedInWater() => false;

    /// <inheritdoc />
    public override bool canStackWith(ISalable other)
    {
        if (other is not InventoryGiantCrop otherBush)
        {
            return false;
        }
        return this.ParentSheetIndex == otherBush.ParentSheetIndex
            && this.Category == otherBush.Category
            && this.stringID.Value == otherBush.stringID.Value;
    }
    #endregion

    #region helpers

    private static int ProductToGameIndex(int productIndex)
        => productIndex switch
        {
            190 => GiantCrop.cauliflower,
            254 => GiantCrop.melon,
            276 => GiantCrop.pumpkin,
            _ => productIndex,
        };

    #endregion

}
