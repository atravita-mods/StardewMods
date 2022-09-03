using LastDayToPlantRedux.Framework;
using StardewModdingAPI.Events;

namespace LastDayToPlantRedux;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    internal static IMonitor ModMonitor { get; private set; } = null!;

    internal static IGameContentHelper GameContentHelper { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // bind helpers.
        ModMonitor = this.Monitor;
        

        helper.Events.GameLoop.DayStarted += this.OnDayStart;
        helper.Events.Multiplayer.PeerConnected += (_, e) => MultiplayerManager.OnPlayerConnected(e);
        helper.Events.Multiplayer.PeerDisconnected += (_, e) => MultiplayerManager.OnPlayerDisconnected(e);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    private void OnDayStart(object? sender, DayStartedEventArgs e)
    {
         MultiplayerManager.UpdateOnDayStart(e);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        => MultiplayerManager.SetShouldCheckPrestiged(this.Helper.ModRegistry);
}
