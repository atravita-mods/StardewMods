using xTile.Dimensions;

namespace AtraCore.Framework.ActionCommandHandler;

/// <summary>
/// An action command to teleport players.
/// </summary>
internal static class TeleportPlayer
{
    // <string area> <int x> <int y> [string prerequisite]

    /// <summary>
    /// Moves the player to a different location.
    /// </summary>
    /// <param name="loc">Game location this was called from.</param>
    /// <param name="parameters">Parameters.</param>
    /// <param name="who">The farmer in question.</param>
    /// <param name="location">tile location.</param>
    /// <returns>True if handled, false otherwise.</returns>
    internal static bool ApplyCommand(GameLocation loc, ArraySegment<string> parameters, Farmer who, Location location)
    {
        if (!who.IsLocalPlayer)
        {
            return false;
        }
        if (parameters.Count is not 3 or 4 or 5)
        {
            ModEntry.ModMonitor.LogOnce($"Expected 3-5 params, {string.Join(' ', parameters)}", LogLevel.Warn);
            return false;
        }

        GameLocation? destination = Game1.getLocationFromName(parameters[0]);
        if (destination is null)
        {
            ModEntry.ModMonitor.Log($"Could not find location {parameters[0]} for warp", LogLevel.Warn);
            return false;
        }

        if (!int.TryParse(parameters[1], out int x) || !int.TryParse(parameters[2], out int y))
        {
            ModEntry.ModMonitor.Log($"Could not parse destination ({parameters[1]}, {parameters[2]}) for warp", LogLevel.Warn);
            return false;
        }

        int? direction = null;
        if (parameters.Count > 3)
        {
            string? mail = null;
            if (parameters.Count == 5)
            {
                if (!int.TryParse(parameters[3], out int facing))
                {
                    ModEntry.ModMonitor.Log($"Could not parse direction ({parameters[3]}) for warp", LogLevel.Warn);
                    return false;
                }

                direction = facing;
                mail = parameters[4];
            }
            else if (parameters.Count == 4)
            {
                if (!int.TryParse(parameters[3], out int facing))
                {
                    mail = parameters[3];
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
