﻿using AtraShared.Integrations.GMCMAttributes;

using Microsoft.Xna.Framework;

using NetEscapades.EnumGenerators;

namespace GrowableGiantCrops.Framework;

/// <summary>
/// The configuration class for this mod.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Reviewed.")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Fields kept near accessors.")]
public sealed class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether relaxed placement rules should be allowed.
    /// </summary>
    public bool RelaxedPlacement { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether shovel can only lift large items you've placed.
    /// </summary>
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
    /// Gets or sets a value indicating whether or not the shovel should do damage to monsters.
    /// </summary>
    public bool ShovelDoesDamage { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not the shovel should be allowed the two hoe enchantments.
    /// </summary>
    public bool AllowHoeEnchantments { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not shops should have graphics.
    /// </summary>
    [GMCMSection("Shop", -10)]
    public bool ShowShopGraphics { get; set; } = true;

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

    private int maxGiantCropsSold = 5;

    /// <summary>
    /// Gets or sets the maximum number of giant crops that will be sold in the giant crop store,
    /// if perfection has not been reached.
    /// </summary>
    [GMCMRange(1, 25)]
    [GMCMSection("Shop", -10)]
    public int MaxGiantCropsSold
    {
        get => this.maxGiantCropsSold;
        set => this.maxGiantCropsSold = Math.Max(value, 1);
    }

    /// <summary>
    /// Gets or sets a value indicating whether or not NPCs should trample placed resource clumps.
    /// </summary>
    [GMCMSection("LargeItems", 0)]
    public bool ShouldNPCsTrampleResourcesClumps { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not NPCs should be able to trample giant crops.
    /// </summary>or
    [GMCMSection("LargeItems", 0)]
    public bool ShouldNPCsTrampleGiantCrops { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not moddata should be copied.
    /// </summary>
    [GMCMSection("LargeItems", 0)]
    public bool PreserveModData { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether large inventory items, like giant crops and trees
    /// should be allowed to stack.
    /// </summary>
    [GMCMSection("LargeItems", 0)]
    public bool AllowLargeItemStacking { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether placed trees should spread.
    /// </summary>
    [GMCMSection("Trees", 5)]
    public bool PlacedTreesSpread { get; set; } = false;

    private int maxTreeStage = 3;

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

    private int maxFruitTreeStage = 3;

    /// <summary>
    /// Gets or sets the maximum state of a fruit tree that can be lifted by the shovel.
    /// Matches the stages shown on the wiki.
    /// </summary>
    [GMCMRange(0, 5)]
    [GMCMSection("Trees", 5)]
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

    /// <summary>
    /// Gets the maximum stage of a fruit tree that can be lifted by the shovel.
    /// Used internally, matches game fruit tree stages.
    /// </summary>
    internal int MaxFruitTreeStageInternal => this.maxFruitTreeStage;

    /// <summary>
    /// Gets or sets a value indicating how palm trees should behave.
    /// </summary>
    [GMCMSection("Trees", 5)]
    public PalmTreeBehavior PalmTreeBehavior { get; set; } = PalmTreeBehavior.Seasonal;

    /// <summary>
    /// Gets or sets a value indicating whether or not animals should ignore placed grass.
    /// </summary>
    [GMCMSection("Grass", 10)]
    public bool ShouldAnimalsEatPlacedGrass { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not placed grass should be capable of spreading.
    /// </summary>
    [GMCMSection("Grass", 10)]
    public bool ShouldPlacedGrassSpread { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not placed grass should ignore the scythe.
    /// </summary>
    [GMCMSection("Grass", 10)]
    public bool ShouldPlacedGrassIgnoreScythe { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not placed slime balls can be squished.
    /// </summary>
    [GMCMSection("Misc", 20)]
    public bool CanSquishPlacedSlimeBalls { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not placed weeds should die in winter.
    /// </summary>
    [GMCMSection("Misc", 20)]
    public bool PreservePlacedWeeds { get; set; } = true;
}

/// <summary>
/// An enum indicating how palm trees should behave.
/// </summary>
[Flags]
[EnumExtensions]
public enum PalmTreeBehavior
{
    /// <summary>
    /// Follows normal game logic (no changes).
    /// </summary>
    Default = 0,

    /// <summary>
    /// Uses seasonal textures in fall and winter.
    /// </summary>
    Seasonal = 1,

    /// <summary>
    /// Turns into a stump in winter, and back again in spring.
    /// </summary>
    Stump = 1 << 1,

    /// <summary>
    /// Uses both behaviors.
    /// </summary>
    Both = Stump | Seasonal,
}