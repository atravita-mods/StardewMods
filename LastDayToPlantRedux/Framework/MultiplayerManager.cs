using StardewModdingAPI.Events;

namespace LastDayToPlantRedux.Framework;

/// <summary>
/// Manages multiplayer stuff for this mod.
/// </summary>
internal static class MultiplayerManager
{
    /// <summary>
    /// Whether or het the code should check for a prestiged agriculturalist, for Walk of Life.
    /// </summary>
    private static bool shouldCheckPrestiged = false;

    /// <summary>
    /// Gets the farmer used as the agriculturalist farmer.
    /// </summary>
    internal static Farmer? AgriculturalistFarmer { get; private set; } = null;

    /// <summary>
    /// Gets the farmer used as the prestiged agriculturalist farmer.
    /// </summary>
    internal static Farmer? PrestigedAgriculturalistFarmer { get; private set; } = null;

    /// <summary>
    /// Checks to see if WoL is installed.
    /// </summary>
    /// <param name="registry">ModRegistry.</param>
    internal static void SetShouldCheckPrestiged(IModRegistry registry)
    {
        shouldCheckPrestiged = registry.IsLoaded("DaLion.ImmersiveProfessions");
    }

    internal static void Reset()
    {
        AgriculturalistFarmer = null;
        PrestigedAgriculturalistFarmer = null;
    }

    internal static void UpdateOnDayStart(DayStartedEventArgs e)
    {
        AgriculturalistFarmer = null;
        PrestigedAgriculturalistFarmer = null;
        if (!Context.IsMultiplayer)
        {
            _ = AssignProfessionFarmersIfNeeded(Game1.player);
        }
        else if (Context.ScreenId == 0)
        {
            foreach (var farmer in Game1.getOnlineFarmers())
            {
                _ = AssignProfessionFarmersIfNeeded(farmer);
            }
        }
    }

    internal static void OnPlayerConnected(PeerConnectedEventArgs e)
    {
        Farmer farmer = Game1.getFarmer(e.Peer.PlayerID);
        _ = AssignProfessionFarmersIfNeeded(farmer);
    }

    internal static void OnPlayerDisconnected(PeerDisconnectedEventArgs e)
    {
        if (e.Peer.PlayerID == AgriculturalistFarmer?.UniqueMultiplayerID)
        {
            AgriculturalistFarmer = null;
        }

        if (shouldCheckPrestiged && e.Peer.PlayerID == PrestigedAgriculturalistFarmer?.UniqueMultiplayerID)
        {
            PrestigedAgriculturalistFarmer = null;
        }

        var farmers = Game1.getAllFarmers().GetEnumerator();


        while ((AgriculturalistFarmer is null || PrestigedAgriculturalistFarmer is null) &&
            farmers.MoveNext())
        {
            _ = AssignProfessionFarmersIfNeeded(farmers.Current);
        }
    }

    private static bool AssignProfessionFarmersIfNeeded(Farmer farmer)
    {
        if (shouldCheckPrestiged && PrestigedAgriculturalistFarmer is null && farmer.professions.Contains(Farmer.agriculturist + 100))
        {
            ModEntry.ModMonitor.Log($"Assigning {farmer.Name} as prestiged agricultralist farmer.");
            PrestigedAgriculturalistFarmer = farmer;
            return true;
        }
        else if (AgriculturalistFarmer is null && farmer.professions.Contains(Farmer.agriculturist)
            && !farmer.professions.Contains(Farmer.agriculturist + 100))
        {
            ModEntry.ModMonitor.Log($"Assigning {farmer.Name} as argicultralist farmer.");
            AgriculturalistFarmer = farmer;
            return true;
        }

        return false;
    }
}
