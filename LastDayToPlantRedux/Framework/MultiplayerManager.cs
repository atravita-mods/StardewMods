using StardewModdingAPI.Events;

namespace LastDayToPlantRedux.Framework;

internal static class MultiplayerManager
{
    internal static Farmer? agriculturalistFarmer { get; private set; } = null;

    internal static Farmer? prestigedAgriculturalistFarmer { get; private set; } = null;

    /// <summary>
    /// Whether or het the code should check for a prestiged agriculturalist, for Walk of Life.
    /// </summary>
    private static bool ShouldCheckPrestiged = false;

    internal static void SetShouldCheckPrestiged(IModRegistry registry)
    {
        ShouldCheckPrestiged = registry.IsLoaded("DaLion.ImmersiveProfessions");
    }

    internal static void UpdateOnDayStart(DayStartedEventArgs e)
    {
        agriculturalistFarmer = null;
        prestigedAgriculturalistFarmer = null;
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
        if (e.Peer.PlayerID == agriculturalistFarmer?.UniqueMultiplayerID)
        {
            agriculturalistFarmer = null;
        }

        if (ShouldCheckPrestiged && e.Peer.PlayerID == prestigedAgriculturalistFarmer?.UniqueMultiplayerID)
        {
            prestigedAgriculturalistFarmer = null;
        }

        var farmers = Game1.getAllFarmers().GetEnumerator();


        while ((agriculturalistFarmer is null || prestigedAgriculturalistFarmer is null) &&
            farmers.MoveNext())
        {
            _ = AssignProfessionFarmersIfNeeded(farmers.Current);
        }
    }

    private static bool AssignProfessionFarmersIfNeeded(Farmer farmer)
    {
        if (ShouldCheckPrestiged && prestigedAgriculturalistFarmer is null && farmer.professions.Contains(Farmer.agriculturist + 100))
        {
            ModEntry.ModMonitor.Log($"Assigning {farmer.Name} as prestiged agricultralist farmer.");
            prestigedAgriculturalistFarmer = farmer;
            return true;
        }
        else if (agriculturalistFarmer is null && farmer.professions.Contains(Farmer.agriculturist)
            && !farmer.professions.Contains(Farmer.agriculturist + 100))
        {
            ModEntry.ModMonitor.Log($"Assigning {farmer.Name} as argicultralist farmer.");
            agriculturalistFarmer = farmer;
            return true;
        }

        return false;
    }
}
