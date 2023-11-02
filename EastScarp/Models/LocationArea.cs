namespace EastScarp.Models;

using Microsoft.Xna.Framework;

public abstract class LocationArea : BaseEntry
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

public sealed class WaterColor : BaseEntry
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