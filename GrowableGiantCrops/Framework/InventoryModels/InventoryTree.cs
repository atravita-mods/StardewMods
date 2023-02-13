using System.Xml.Serialization;

using AtraCore.Framework.ReflectionManager;

using GrowableGiantCrops.Framework.Assets;

using Microsoft.Xna.Framework;

using Netcode;

using StardewValley.Locations;
using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops.Framework.InventoryModels;

/// <summary>
/// A class that represents a normal tree in the inventory.
/// </summary>
[XmlType("Mods_atravita_InventoryTree")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Keeping like methods together.")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Keeping like methods together.")]
public sealed class InventoryTree : SObject
{
    #region consts

    /// <summary>
    /// A prefix used on the name of a tree in the inventory.
    /// </summary>
    internal const string InventoryTreePrefix = "atravita.InventoryTree/";

    /// <summary>
    /// The category number for inventory trees.
    /// </summary>
    internal const int InventoryTreeCategory = -645547;
    #endregion

    /// <summary>
    /// The growth stage.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Public for serializer.")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "Reviewed.")]
    public readonly NetInt growthStage = new(Tree.seedStage);

    #region drawfields
    [XmlIgnore]
    private AssetHolder? holder;

    [XmlIgnore]
    private Rectangle sourceRect = default;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryTree"/> class.
    /// This is for the serializer, do not use.
    /// </summary>
    public InventoryTree()
        : base()
    {
        this.NetFields.AddField(this.growthStage);
        this.Category = InventoryTreeCategory;
        this.Price = 0;
        this.CanBeSetDown = true;
        this.Edibility = inedible;
        this.bigCraftable.Value = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryTree"/> class.
    /// </summary>
    /// <param name="idx">The index of the tree.</param>
    /// <param name="initialStack">The initial stack.</param>
    /// <param name="growthStage">The growth stage.</param>
    public InventoryTree(TreeIndexes idx, int initialStack, int growthStage)
        : this()
    {
        if (!TreeIndexesExtensions.IsDefined(idx))
        {
            ModEntry.ModMonitor.Log($"Tree {idx.ToStringFast()} doesn't seem to be a valid tree. Setting to pine tree.", LogLevel.Error);
            idx = TreeIndexes.Pine;
        }

        this.ParentSheetIndex = (int)idx;
        this.growthStage.Value = growthStage;
        this.Stack = initialStack;
        this.Name = InventoryTreePrefix + idx.ToStringFast();
    }

    #region reflection
    /// <summary>
    /// Stardew's Tree::shake.
    /// </summary>
    internal static readonly TreeShakeDel TreeShakeMethod = typeof(Tree)
        .GetCachedMethod("shake", ReflectionCache.FlagTypes.InstanceFlags)
        .CreateDelegate<TreeShakeDel>();

    /// <summary>
    /// A delegate that matches Tree.shake's call pattern.
    /// </summary>
    /// <param name="tree">The tree to shake.</param>
    /// <param name="tileLocation">the tile location of the tree.</param>
    /// <param name="doEvenIfStillShaking">Whether or not to shake the tree even if it's still shaking.</param>
    /// <param name="location">The relevant game location.</param>
    internal delegate void TreeShakeDel(
        Tree tree,
        Vector2 tileLocation,
        bool doEvenIfStillShaking,
        GameLocation location);
    #endregion

    #region placement

    /// <inheritdoc />
    public override bool canBePlacedHere(GameLocation l, Vector2 tile)
        => this.CanPlace(l, tile, ModEntry.Config.RelaxedPlacement);

    /// <summary>
    /// Checks to see if a tree can be placed here.
    /// </summary>
    /// <param name="l">The game location.</param>
    /// <param name="tile">The tile to place at.</param>
    /// <param name="relaxed">Whether or not relaxed placement rules should be used.</param>
    /// <returns>True if placement should be allowed, false otherwise.</returns>
    internal bool CanPlace(GameLocation l, Vector2 tile, bool relaxed)
    {
        TreeIndexes tree = (TreeIndexes)this.ParentSheetIndex;
        if (tree == TreeIndexes.Invalid || !TreeIndexesExtensions.IsDefined(tree))
        {
            return false;
        }

        int x = (int)tile.X;
        int y = (int)tile.Y;

        return (GGCUtils.CanPlantTreesAtLocation(l, relaxed, x, y) || l.CanPlantTreesHere(69, x, y)) // 69 - banana tree.
            && l.terrainFeatures?.ContainsKey(tile) == false
            && GGCUtils.IsTilePlaceableForResourceClump(l, x, y, relaxed);
    }

    /// <inheritdoc />
    public override bool placementAction(GameLocation location, int x, int y, Farmer? who = null)
        => this.PlaceTree(location, x, y, ModEntry.Config.RelaxedPlacement);

    internal bool PlaceTree(GameLocation location, int x, int y, bool relaxed)
    {
        Vector2 placementTile = new(x / Game1.tileSize, y / Game1.tileSize);
        if (!this.CanPlace(location, placementTile, relaxed))
        {
            return false;
        }

        Tree tree = new(this.ParentSheetIndex, this.growthStage.Value);
        location.terrainFeatures[placementTile] = tree;
        TreeShakeMethod(tree, placementTile, true, location);
        location.playSound("dirtyHit");
        DelayedAction.playSoundAfterDelay("coin", 100);
        return true;
    }

    #endregion

    #region helpers

    /// <summary>
    /// Resets the draw fields.
    /// </summary>
    internal void Reset()
    {
        // this is necessary for seasons.
        this.holder = null;
        this.sourceRect = default;
    }

    /// <summary>
    /// Populates the fields required for drawing for this particular location.
    /// </summary>
    /// <param name="loc">Gamelocation.</param>
    internal void PopulateDrawFields(GameLocation? loc = null)
    {
        loc ??= Game1.currentLocation;
        if (loc is null)
        {
            return;
        }

        // derived from Tree.loadTexture and Tree.draw
        string season = loc is Desert or MineShaft ? "spring" : Game1.GetSeasonForLocation(loc);

        string assetPath;
        switch (this.ParentSheetIndex)
        {
            case Tree.mushroomTree:
                assetPath = @"TerrainFeatures\mushroom_tree";
                break;
            case Tree.palmTree:
                assetPath = @"TerrainFeatures\tree_palm";
                break;
            case Tree.palmTree2:
                assetPath = @"TerrainFeatures\tree_palm2";
                break;
            case Tree.pineTree:
                if (season == "summer")
                {
                    assetPath = @"TerrainFeatures\tree3_spring";
                    break;
                }
                goto default;
            default:
                assetPath = $@"TerrainFeatures\tree{this.ParentSheetIndex}_{season}";
                break;
        }

        this.holder = AssetCache.Get(assetPath);
        if (this.holder is null)
        {
            return;
        }

        this.sourceRect = this.growthStage.Value switch
        {
            0 => new Rectangle(32, 128, 16, 16),
            1 => new Rectangle(0, 128, 16, 16),
            2 => new Rectangle(16, 128, 16, 16),
            3 => new Rectangle(0, 96, 16, 32),
            _ => new Rectangle(0, 0, 48, 96),
        };
    }
    #endregion
}
