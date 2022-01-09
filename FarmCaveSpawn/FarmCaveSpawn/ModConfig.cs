namespace FarmCaveSpawn;
class ModConfig
{
    /// <summary>
    /// Maximum number of spawns per day.
    /// </summary>
    public int MaxDailySpawns { get; set; } = 6;

    /// <summary>
    /// Probability of any tile spawning an object, capped by max daily spawns
    /// </summary>
    public float SpawnChance { get; set; } = 3f;

    /// <summary>
    /// Probability of any particular spawn being a tree fruit item.
    /// </summary>
    public float TreeFruitChance { get; set; } = 20f;

    /// <summary>
    /// Should spawn in fruit after the Demetrius cutscene is seen, regardless of choice.
    /// </summary>
    /// <remarks>Checks for caveChoice, but also FarmCaveFarmework</remarks>
    public bool IgnoreFarmCaveType { get; set; } = false;

    /// <summary>
    /// Should I allow fruit spawning even before Demeterius shows up.
    /// </summary>
    /// <remarks>Checks for caveChoice, but also FarmCaveFramework</remarks>
    public bool EarlyFarmCave { get; set; } = false;
    
    /// <summary>
    /// Should I check the additional locations list?
    /// </summary>
    public bool UseModCaves { get; set; } = true;

    /// <summary>
    /// Should I use the mine cave entrance as well?
    /// </summary>
    public bool UseMineCave { get; set; } = false;

    /// <summary>
    /// Should I limit myself to just fruits in season?
    /// </summary>
    public bool SeasonalOnly { get; set; } = false;

    /// <summary>
    /// Should I allow any fruit tree product, even if it's not categorized as fruit?
    /// </summary>
    public bool AllowAnyTreeProduct { get; set; } = true;

    /// <summary>
    /// Restrict to only edible items.
    /// </summary>
    /// <remarks>Sometimes inexplicable things in the game have positive edibility....</remarks>
    public bool EdiblesOnly { get; set; } = true;

    /// <summary>
    /// Remove bananas from the pool before a specific vanilla quest is done.
    /// </summary>
    public bool NoBananasBeforeShrine { get; set; } = true;

    /// <summary>
    /// Caps the price of fruit you can get
    /// </summary>
    public int PriceCap { get; set; } = 200;
}