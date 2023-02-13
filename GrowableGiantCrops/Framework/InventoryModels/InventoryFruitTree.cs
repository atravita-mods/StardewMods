using System.Xml.Serialization;

using AtraBase.Toolkit.Extensions;

using GrowableGiantCrops.Framework.Assets;

using Microsoft.Xna.Framework;

using Netcode;

using StardewValley.Locations;
using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops.Framework.InventoryModels;

/// <summary>
/// A class that represents a fruit tree in the inventory.
/// </summary>
[XmlType("Mods_atravita_InventoryFruitTree")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Keeping like methods together.")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Keeping like methods together.")]
public sealed class InventoryFruitTree : SObject
{
    #region consts

    /// <summary>
    /// A prefix used on the name of a tree in the inventory.
    /// </summary>
    internal const string InventoryTreePrefix = "atravita.InventoryFruitTree/";

    /// <summary>
    /// The category number for inventory trees.
    /// </summary>
    internal const int InventoryTreeCategory = -645548;
    #endregion

    /// <summary>
    /// The growth stage.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Public for serializer.")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "Reviewed.")]
    public readonly NetInt growthStage = new(FruitTree.seedStage);

    /// <summary>
    /// The number of days until the fruit tree is mature.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Public for serializer.")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "Reviewed.")]
    public readonly NetInt daysUntilMature = new(28);

    /// <summary>
    /// Whether or not the tree has been struck by lightning.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Public for serializer.")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "Reviewed.")]
    public readonly NetBool struckByLightning = new(false);

    [XmlIgnore]
    private Rectangle sourceRect = default;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryFruitTree"/> class.
    /// This is for the serializer, do not use.
    /// </summary>
    public InventoryFruitTree()
        : base()
    {
        this.NetFields.AddFields(this.daysUntilMature, this.struckByLightning, this.growthStage);
        this.Category = InventoryTreeCategory;
        this.Price = 0;
        this.CanBeSetDown = true;
        this.Edibility = inedible;
        this.bigCraftable.Value = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryFruitTree"/> class.
    /// </summary>
    /// <param name="saplingIndex">The index of the sapling the tree corresponds to.</param>
    /// <param name="initialStack">Initial stack.</param>
    /// <param name="growthStage">Growth stage of the tree.</param>
    /// <param name="daysUntilMature">Number of days until the tree is mature.</param>
    /// <param name="struckByLightning">Whether or not the tree has been struck by lightning.</param>
    public InventoryFruitTree(int saplingIndex, int initialStack, int growthStage,  int daysUntilMature, bool struckByLightning)
        : this()
    {
        Dictionary<int, string> data = Game1.content.Load<Dictionary<int, string>>(@"Data\fruitTrees");
        if (!data.ContainsKey(saplingIndex))
        {
            int replacement = data.Keys.FirstOrDefault();
            ModEntry.ModMonitor.Log($"Tree {saplingIndex} doesn't seem to be a valid tree. Setting to default: {replacement}", LogLevel.Error);
            saplingIndex = replacement;
        }

        this.ParentSheetIndex = saplingIndex;
        this.daysUntilMature.Value = daysUntilMature;
        this.struckByLightning.Value = struckByLightning;
        this.growthStage.Value = growthStage;
        this.Stack = initialStack;
        this.Name = InventoryTreePrefix + GGCUtils.GetNameOfSObject(saplingIndex);
    }

    #region placement

    /// <inheritdoc />
    public override bool canBePlacedHere(GameLocation l, Vector2 tile)
        => CanPlace(l, tile, ModEntry.Config.RelaxedPlacement);

    /// <summary>
    /// Checks to see if a tree can be placed here.
    /// </summary>
    /// <param name="l">The game location.</param>
    /// <param name="tile">The tile to place at.</param>
    /// <param name="relaxed">Whether or not relaxed placement rules should be used.</param>
    /// <returns>True if placement should be allowed, false otherwise.</returns>
    internal static bool CanPlace(GameLocation l, Vector2 tile, bool relaxed)
    {
        int x = (int)tile.X;
        int y = (int)tile.Y;
        return (GGCUtils.CanPlantTreesAtLocation(l, relaxed, x, y) || l.CanPlantTreesHere(69, x, y)) // 69 - banana tree.
            && l.terrainFeatures?.ContainsKey(tile) == false
            && GGCUtils.IsTilePlaceableForResourceClump(l, x, y, relaxed)
            && (relaxed || !FruitTree.IsGrowthBlocked(tile, l));
    }

    #endregion

    #region helpers

    /// <summary>
    /// resets the source rectangle, used to transition between maps of different seasons.
    /// </summary>
    internal void Reset()
    {
        if (this.growthStage.Value >= FruitTree.treeStage)
        {
            this.sourceRect = default;
        }
    }

    /// <summary>
    /// Populates the fields required for drawing for this fruit tree.
    /// </summary>
    /// <param name="loc">The game location, or null for current.</param>
    internal void PopulateDrawFields(GameLocation? loc = null)
    {
        loc ??= Game1.currentLocation;
        if (loc is null)
        {
            return;
        }

        Dictionary<int, string> data = Game1.content.Load<Dictionary<int, string>>(@"Data\fruitTrees");
        if (!data.TryGetValue(this.ParentSheetIndex, out string? treeInfo)
            || !int.TryParse(treeInfo.GetNthChunk('/'), out int treeIndex))
        {
            return;
        }

        // derived from FruitTree.draw
        int season = Utility.getSeasonNumber(loc is Desert or MineShaft ? "spring" : Game1.GetSeasonForLocation(loc));

        const int HEIGHT = 80;
        const int WIDTH = 48;
        this.sourceRect = this.growthStage.Value switch
        {
            0 => new Rectangle(0, treeIndex * HEIGHT, WIDTH, HEIGHT),
            1 => new Rectangle(WIDTH, treeIndex * HEIGHT, WIDTH, HEIGHT),
            2 => new Rectangle(WIDTH * 2, treeIndex * HEIGHT, WIDTH, HEIGHT),
            3 => new Rectangle(WIDTH * 3, treeIndex * HEIGHT, WIDTH, HEIGHT),
            _ => new Rectangle((season * WIDTH) + 192, treeIndex * HEIGHT, WIDTH, HEIGHT),
        };
    }

    #endregion
}
