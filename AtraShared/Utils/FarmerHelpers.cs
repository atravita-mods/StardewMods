namespace AtraShared.Utils;

public static class FarmerHelpers
{
    public static IEnumerable<Farmer> GetFarmers()
    => Game1.getAllFarmers().Where(f => f is not null);

    public static bool HasAnyFarmerRecievedFlag(string flag)
    {
        foreach (Farmer farmer in GetFarmers())
        {
            if (farmer.hasOrWillReceiveMail(flag))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets a farmer by their unique multiplayer ID.
    /// </summary>
    /// <param name="id">Multiplayer ID.</param>
    /// <returns>Farmer, or null if not found.</returns>
    public static Farmer? GetFarmerById(long id)
    {
        if (Game1.player.UniqueMultiplayerID == id)
        {
            return Game1.player;
        }

        foreach (Farmer? player in Game1.otherFarmers.Values)
        {
            if (player.UniqueMultiplayerID == id)
            {
                return player;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets a farmer by name.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <returns>Farmer, or null if not found.</returns>
    /// <remarks>If there are multiple farmers by the same name, gets the first. Does prefer Game1.player.</remarks>
    public static Farmer? GetFarmerByName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        if (Game1.player.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            return Game1.player;
        }

        foreach (Farmer? player in Game1.otherFarmers.Values)
        {
            if (player.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return player;
            }
        }

        return null;
    }
}