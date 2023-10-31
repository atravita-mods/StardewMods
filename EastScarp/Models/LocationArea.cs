namespace EastScarp.Models;

using Microsoft.Xna.Framework;

using StardewValley;
using StardewValley.Extensions;

/// <summary>
/// The base class for an entry.
/// </summary>
public abstract class Entry
{
    /// <summary>
    /// Gets or sets the <see cref="GameStateQuery"/> to check, or null for always true.
    /// </summary>
    public string? Conditions { get; set; }

    /// <summary>
    /// Gets or sets the chance this entry should apply.
    /// </summary>
    public float Chance { get; set; } = 0f;

    /// <summary>
    /// Checks to see if the conditions associated with this entry are satisfied.
    /// </summary>
    /// <param name="location">The game location to use, or null for current location.</param>
    /// <param name="player">The player to use, or null for current player.</param>
    /// <returns>True if allowed, false otherwise.</returns>
    public bool CheckCondition(GameLocation? location, Farmer? player)
    {
        player ??= Game1.player;
        location ??= Game1.currentLocation ?? player.currentLocation;

        if (location is null || Random.Shared.NextBool(this.Chance))
        {
            return false;
        }
        return GameStateQuery.CheckConditions(this.Conditions, location, player);
    }
}

public abstract class LocationArea : Entry
{
    /// <summary>
    /// The rectangular area to check. Defaults to the whole map.
    /// </summary>
    public Rectangle Area { get; set; } = new(0, 0, -1, -1);

    /// <summary>Checks to see if the point exists within the rectangle.</summary>
    internal bool Contains(Point point)
    {
        if (point.X < this.Area.X || point.Y < this.Area.Y)
        {
            return false;
        }
        if (this.Area.Height != -1 && point.X > this.Area.Right)
        {
            return false;
        }
        if (this.Area.Width != -1 && point.Y > this.Area.Bottom)
        {
            return false;
        }

        return true;
    }
}

public sealed class AmbientSound: LocationArea
{
    public SpawnTrigger Trigger { get; set; }
    public string Sound { get; set; } = string.Empty;
}

/// <summary>
/// Represents a range, inclusive.
/// </summary>
public struct RRange
{
    public RRange()
    {
    }

    public RRange(int min, int max)
    {
        this.Min = min;
        this.Max = max;
    }

    int Min { get; set; } = 1;
    int Max { get; set; } = 1;
}

/// <summary>
/// The event to trigger the spawn at.
/// </summary>
[Flags]
public enum SpawnTrigger
{
    /// <summary>
    /// When the player enters the map.
    /// </summary>
    OnEntry = 0x1,
    
    /// <summary>
    /// When the clock changes.
    /// </summary>
    OnTimeChange = 0x2,

    /// <summary>
    /// Every second.
    /// </summary>
    OnSecond = 0x4,

    /// <summary>
    /// Every tick (60x a second.) Use sparingly.
    /// </summary>
    OnTick = 0x8,
}

public enum CritterType
{
    // these can spawn off the map
    OverheadParrot,
    Butterfly,
    IslandButterfly,
    Cloud,
    Owl,

    // these should probably spawn ON the map.
    BrownBird,
    BlueBird,
    SpecialBlueBird,
    SpecialRedBird,
    CalderaMonkey,
    Crab,
    Crow,
    Firefly,
    Frog,
    Rabbit,
    Seagull,
    Squirrel,
    Woodpecker
}

public sealed class CritterSpawn: LocationArea
{
    public SpawnTrigger Trigger { get; set; } = SpawnTrigger.OnEntry;

    public CritterType Critter { get; set; }

    public float ChanceOnLand { get; set; }

    public float ChanceOnWater { get; set; }

    public RRange Clusters { get; set; }

    public RRange CountPerCluster { get; set; }
}

public sealed class SeaMonsterSpawn : LocationArea
{
    public SpawnTrigger Trigger { get; set; } = SpawnTrigger.OnEntry;

    public float Chance { get; set; } = 0f;
}

public sealed class WaterColor : Entry
{
    public string Color { get; set; } = string.Empty;
}

public sealed class Model
{
    public List<AmbientSound> Sounds { get; set; } = new();
    public List<SeaMonsterSpawn> SeaMonsterSpawn { get; set; } = new();

    public List<WaterColor> WaterColor { get; set; } = new();

    public List<CritterSpawn> Critters { get; set; } = new();
}