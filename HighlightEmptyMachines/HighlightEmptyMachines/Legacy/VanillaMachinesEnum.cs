using AtraBase.Toolkit;

namespace HighlightEmptyMachines.Legacy;

/// <summary>
/// Legacy enum for all the vanilla machines, created simply
/// </summary>
/// <remarks>Positive numbers refer to Big Craftables, negative ordinary SObjects.</remarks>
[Obsolete("Vanilla machines were de-hardcoded in 1.6")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items should be documented", Justification = StyleCopErrorConsts.SelfEvident)]
public enum VanillaMachinesEnum
{
    /// <summary>
    /// Beehouse.
    /// </summary>
    BeeHouse = 10,

    /// <summary>
    /// Keg.
    /// </summary>
    Keg = 12,

    /// <summary>
    /// Furnace.
    /// </summary>
    Furnace = 13,

    /// <summary>
    /// Preserves Jar.
    /// </summary>
    PreservesJar = 15,

    /// <summary>
    /// Cheese Press.
    /// </summary>
    CheesePress = 16,

    /// <summary>
    /// Loom.
    /// </summary>
    Loom = 17,

    /// <summary>
    /// Oil maker.
    /// </summary>
    OilMaker = 19,

    /// <summary>
    /// Recycling machine.
    /// </summary>
    RecyclingMachine = 20,

    /// <summary>
    /// Crystalarium.
    /// </summary>
    Crystalarium = 21,

    /// <summary>
    /// Mayo machine.
    /// </summary>
    MayonnaiseMachine = 24,

    /// <summary>
    /// Seed maker.
    /// </summary>
    SeedMaker = 25,
    BoneMill = 90,
    Incubator = 101,
    CharcoalKiln = 114,
    SlimeIncubator = 156,
    SlimeEggPress = 158,
    Cask = 163,
    GeodeCrusher = 182,
    WoodChipper = 211,
    OstrichIncubator = 254,
    Deconstructor = 265,
    CrabPot = -710,
}