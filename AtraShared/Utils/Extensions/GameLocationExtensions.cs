// Ignore Spelling: viewport Hoedirt

using AtraBase.Toolkit.Extensions;

using CommunityToolkit.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

using XLocation = xTile.Dimensions.Location;
using XRectangle = xTile.Dimensions.Rectangle;

namespace AtraShared.Utils.Extensions;

/// <summary>
/// Extensions on GameLocation.
/// </summary>
public static class GameLocationExtensions
{
    /// <summary>
    /// Should this location be considered dangerous?
    /// Always safe: Farm, town, IslandWest.
    /// Always dangerous: Volcano, MineShaft.
    /// In-between: everywhere else.
    /// </summary>
    /// <param name="location">Location to check.</param>
    /// <returns>Whether the location should be considered dangerous.</returns>
    [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1008:Opening parenthesis should be spaced correctly", Justification = "Preference.")]
    public static bool IsDangerousLocation(this GameLocation location)
        => !location.IsFarm && !location.IsGreenhouse && location is not (SlimeHutch or Town or IslandWest)
            && (location is MineShaft or VolcanoDungeon or BugLand || location.characters.Any(static (character) => character is Monster));

    /// <summary>
    /// Returns true if there's a festival at a location and the player can't actually warp there yet.
    /// </summary>
    /// <param name="location">Location to check.</param>
    /// <param name="monitor">Logger.</param>
    /// <param name="alertPlayer">Whether or not to show a notification.</param>
    /// <returns>True if there's a festival at this location and it's before the start time, false otherwise.</returns>
    public static bool IsBeforeFestivalAtLocation(this GameLocation location, IMonitor monitor, bool alertPlayer = false)
    {
        Guard.IsNotNull(monitor);
        Guard.IsNotNull(location);

        try
        {
            if (Game1.weatherIcon == 1)
            {
                Dictionary<string, string>? festivalData;
                try
                {
                    festivalData = Game1.temporaryContent.Load<Dictionary<string, string>>($@"Data\Festivals\{Game1.currentSeason}{Game1.dayOfMonth}");
                }
                catch (ContentLoadException)
                {
                    monitor.Log("No festival file found for today....did someone screw with the time?", LogLevel.Warn);
                    return false;
                }
                catch (Exception ex)
                {
                    monitor.Log($"Badly formatted festival file for today:\n\n{ex}", LogLevel.Warn);
                    return false;
                }
                if (festivalData.TryGetValue("conditions", out string? val) && val.TrySplitOnce('/', out ReadOnlySpan<char> locName, out ReadOnlySpan<char> times))
                {
                    if (!locName.Equals(location.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    if (times.TrySplitOnce(' ', out ReadOnlySpan<char> start, out _ )
                        && int.TryParse(start, out int startTime) && Game1.timeOfDay < startTime)
                    {
                        if (alertPlayer)
                        {
                            Game1.drawObjectDialogue(Game1.content.LoadString(@"Strings\StringsFromCSFiles:Game1.cs.2973"));
                        }
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            monitor.LogError("finding festival days", ex);
        }
        return false;
    }

    /// <summary>
    /// Checks to see if hoedirt can be created here. Derived from Hoe.
    /// </summary>
    /// <param name="location">Game location to pick up from.</param>
    /// <param name="tile">Tile to make hoedirt at.</param>
    /// <returns>True if hoedirt can be created, false otherwise.</returns>
    public static bool CanCreateHoedirtAt(this GameLocation location, Vector2 tile)
    {
        Guard.IsNotNull(location);
        return location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Diggable", "Back") is not null
            && !location.IsTileOccupiedBy(tile, CollisionMask.All, CollisionMask.None, useFarmerTile: true)
            && location.isTilePassable(new XLocation((int)tile.X, (int)tile.Y), Game1.viewport);
    }

    /// <summary>
    /// Gets the hoedirt at a specific tile, in a pot or on the ground.
    /// </summary>
    /// <param name="location">Location.</param>
    /// <param name="tile">Tile.</param>
    /// <returns>Hoedirt if found.</returns>
    public static HoeDirt? GetHoeDirtAtTile(this GameLocation location, Vector2 tile)
    {
        Guard.IsNotNull(location);

        if (location.terrainFeatures.TryGetValue(tile, out TerrainFeature? terrain)
            && terrain is HoeDirt dirt)
        {
            return dirt;
        }
        else if (location.Objects.TryGetValue(tile, out SObject obj)
            && obj is IndoorPot pot)
        {
            return pot.hoeDirt.Value;
        }
        return null;
    }

    /// <summary>
    /// Whether or not a tile is covered by a Front or AlwaysFront tile at this location.
    /// </summary>
    /// <param name="location">GameLocation.</param>
    /// <param name="tileLocation">Tile.</param>
    /// <param name="viewport">Viewport.</param>
    /// <returns>True if covered, false otherwise.</returns>
    public static bool IsTileViewable(this GameLocation? location, XLocation tileLocation, XRectangle viewport)
    {
        if (location is null)
        {
            return false;
        }

        return (location.map.GetLayer("Front")?.PickTile(new XLocation(tileLocation.X * 64, tileLocation.Y * 64), viewport.Size)
            ?? location.map.GetLayer("AlwaysFront")?.PickTile(new XLocation(tileLocation.X * 64, tileLocation.Y * 64), viewport.Size)) is null;
    }
}