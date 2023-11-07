namespace EastScarp.Models;

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
