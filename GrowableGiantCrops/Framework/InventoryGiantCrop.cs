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
    internal const string GiantCropTweaksModDataKey = "leclair.giantcroptweaks/Id";
    internal const int GiantCropTweaksIndex = -1337; // Numeric ID to distinguish the giant crop tweaks giant crops.
    internal const int GiantCropCategory = -15577335; // set a large random negative number

    public readonly NetString stringID = new NetString(string.Empty);

    [XmlIgnore]
    private Texture2D? texture;

    [XmlIgnore]
    private Rectangle sourceRect = default;

    [XmlIgnore]
    private string? texturePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryGiantCrop"/> class.
    /// Used for the serializer, do not use.
    /// </summary>
    public InventoryGiantCrop()
        : base()
    {
        this.NetFields.AddFields(this.stringID);
        this.Category = GiantCropCategory;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryGiantCrop"/> class for a GiantCropTweaks giant crop.
    /// </summary>
    /// <param name="stringID">the string id of the giantcroptweaks giant crop</param>
    public InventoryGiantCrop(string stringID)
    {
        this.stringID.Value = stringID;
        this.ParentSheetIndex = GiantCropTweaksIndex;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryGiantCrop"/> class for vanilla/JA giant crops.
    /// </summary>
    /// <param name="intID">Integer ID to use.</param>
    public InventoryGiantCrop(int intID)
    {
        this.ParentSheetIndex = intID;
        if (Game1Wrappers.ObjectInfo.TryGetValue(intID, out var data))
        {
            this.Name = data.GetNthChunk('/').ToString();
        }
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
}
