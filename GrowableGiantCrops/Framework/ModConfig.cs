using AtraShared.Integrations.GMCMAttributes;

using Microsoft.Xna.Framework;

namespace GrowableGiantCrops.Framework;

/// <summary>
/// The configuration class for this mod.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Fields kept near accessors.")]
public sealed class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether relaxed placement rules should be allowed.
    /// </summary>
    public bool RelaxedPlacement { get; set; } = false;

    // TODO: if enabled, the shovel can only move placed items.
    public bool PlacedOnly { get; set; } = false;

    private int shovelEnergy = 7;

    /// <summary>
    /// Gets or sets how much energy the shovel uses.
    /// </summary>
    [GMCMRange(0, 25)]
    public int ShovelEnergy
    {
        get => this.shovelEnergy;
        set => this.shovelEnergy = Math.Clamp(this.shovelEnergy, 0, 25);
    }

    /// <summary>
    /// Gets or sets the tile of the giant crop shop's location.
    /// </summary>
    [GMCMDefaultVector(8, 14)]
    [GMCMSection("Shop", -10)]
    public Vector2 GiantCropShopLocation { get; set; } = new(8, 14);

    // TODO: check positioning with SVE.

    /// <summary>
    /// Gets or sets the tile of the resource shop's location.
    /// </summary>
    [GMCMDefaultVector(6, 19)]
    [GMCMSection("Shop", -10)]
    public Vector2 ResourceShopLocation { get; set; } = new(6, 19);

    // TODO

    /// <summary>
    /// Gets or sets a value indicating whether or not NPCs should trample placed resource clumps.
    /// </summary>
    [GMCMSection("LargeItems", 0)]
    public bool ShouldNPCsTrampleResourcesClumps { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not NPCs should be able to trample giant crops.
    /// </summary>
    [GMCMSection("LargeItems", 0)]
    public bool ShouldNPCsTrampleGiantCrops { get; set; } = true;

    // TODO
    [GMCMSection("LargeItems", 0)]
    public bool PreserveModData { get; set; } = true;

    // TODO
    [GMCMSection("LargeItems", 0)]
    public bool AllowLargeItemStacking { get; set; } = true;

    // TODO - note, internally stored as the matching game stage, displayed as wiki stage.
    private int maxTreeStage = 4;

    /// <summary>
    /// Gets or sets the maximum tree stage that can be lifted by the shovel.
    /// Numbers refer to player friendly ("wiki") stages.
    /// </summary>
    [GMCMRange(0, 5)]
    [GMCMSection("Trees", 5)]
    public int MaxTreeStage
    {
        get
        {
            return this.maxTreeStage == 5 ? 5 : this.maxTreeStage + 1;
        }

        set
        {
            this.maxTreeStage = Math.Clamp(value, 0, 5);
            if (this.maxTreeStage < 5)
            {
                this.maxTreeStage--;
            }
        }
    }

    /// <summary>
    /// Gets the maximum stage of a tree that can be lifted by the shovel.
    /// Used internally, matches game tree stages.
    /// </summary>
    internal int MaxTreeStageInternal => this.maxTreeStage;

    private int maxFruitTreeStage = 4;

    [GMCMRange(0, 5)]
    [GMCMSection("Trees", 4)]
    public int MaxFruitTreeStage
    {
        get
        {
            return this.maxFruitTreeStage + 1;
        }

        set
        {
            this.maxFruitTreeStage = Math.Clamp(value, 0, 5) - 1;
        }
    }

    internal int MaxFruitTreeStageInternal => this.maxFruitTreeStage;

    [GMCMSection("Grass", 10)]
    public bool ShouldAnimalsEatPlacedGrass { get; set; } = true;

    // TODO
    [GMCMSection("Grass", 10)]
    public bool ShouldPlacedGrassSpread { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not placed slime balls can be squished.
    /// </summary>
    [GMCMSection("Misc", 20)]
    public bool CanSquishPlacedSlimeBalls { get; set; } = false;
}
