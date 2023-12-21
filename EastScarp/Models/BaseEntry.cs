namespace EastScarp.Models;

using StardewValley;
using StardewValley.Extensions;

/// <summary>
/// The base class for an entry.
/// </summary>
public abstract class BaseEntry
{
    /// <summary>
    /// Gets or sets the <see cref="GameStateQuery"/> to check, or null for always true.
    /// </summary>
    public string? Conditions { get; set; }

    /// <summary>
    /// Gets or sets the chance this entry should apply.
    /// </summary>
    public float Chance { get; set; } = 1f;

    /// <summary>
    /// Gets or sets a value indicating when this entry should apply.
    /// </summary>
    public TimeRange Time { get; set; } = new (600, 2600);

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

        if (location is null || !Random.Shared.NextBool(this.Chance))
        {
            return false;
        }

        if (Game1.timeOfDay < this.Time.StartTime || Game1.timeOfDay > this.Time.EndTime)
        {
            return false;
        }

        return GameStateQuery.CheckConditions(this.Conditions, location, player);
    }
}
