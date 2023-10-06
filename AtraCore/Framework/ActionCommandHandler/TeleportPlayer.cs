using Microsoft.Xna.Framework;

namespace AtraCore.Framework.ActionCommandHandler;

// TODO

/// <summary>
/// An action command to teleport players.
/// </summary>
internal static class TeleportPlayer
{
    /// <summary>
    /// Moves the player to a different location.
    /// </summary>
    /// <param name="loc">Game location this was called from.</param>
    /// <param name="parameters">Parameters.</param>
    /// <param name="who">The farmer in question.</param>
    /// <param name="point">tile location.</param>
    /// <returns>True if handled, false otherwise.</returns>
    internal static bool ApplyCommand(GameLocation loc, string[] parameters, Farmer who, Point point)
    {
        if (!who.IsLocalPlayer)
        {
            return false;
        }

        if (parameters.Length < 4 || parameters.Length > 6)
        {
            loc.LogTileActionError(parameters, point.X, point.Y, "incorrect number of parameters (expected 4-6)");
            return false;
        }

        // <string area> <int x> <int y> [string prerequisite] [int facing]
        if (!ArgUtility.TryGet(parameters, 1, out string? map, out string? error, false))
        {
            loc.LogTileActionError(parameters, point.X, point.Y, error);
            return false;
        }
        if (Game1.getLocationFromName(map) is not GameLocation destination)
        {
            loc.LogTileActionError(parameters, point.X, point.Y, $"find destination for map {map}");
            return false;
        }

        if (!ArgUtility.TryGetInt(parameters, 2, out var x, out error) || !ArgUtility.TryGetInt(parameters, 3, out var y, out error))
        {

        }

        if (!int.TryParse(parameters[2], out int x) || !int.TryParse(parameters[3], out int y))
        {
            ModEntry.ModMonitor.Log($"Could not parse destination ({parameters[2]}, {parameters[3]}) for warp", LogLevel.Warn);
            return false;
        }

        int? direction = null;
        if (parameters.Length > 4)
        {
            string? mail = null;
            if (parameters.Length == 6)
            {
                if (!int.TryParse(parameters[4], out int facing))
                {
                    ModEntry.ModMonitor.Log($"Could not parse direction ({parameters[4]}) for warp", LogLevel.Warn);
                    return false;
                }

                direction = facing;
                mail = parameters[5];
            }
            else if (parameters.Length == 5)
            {
                if (!int.TryParse(parameters[4], out int facing))
                {
                    mail = parameters[4];
                }
                else
                {
                    direction = facing;
                }
            }

            if (mail is not null && !who.mailReceived.Contains(mail))
            {
                ModEntry.ModMonitor.Log($"Ignoring warp because mail flag '{mail}' not received.");
                return false;
            }
        }

        Game1.warpFarmer(destination.Name, x, y, direction ?? who.FacingDirection, false);
        return true;
    }
}
