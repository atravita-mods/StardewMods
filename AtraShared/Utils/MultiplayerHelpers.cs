namespace AtraShared.Utils;

internal static class MultiplayerHelpers
{
    internal static void AssertMultiplayerVersions(IMultiplayerHelper multi, IManifest manifest, IMonitor monitor, ITranslationHelper translation)
    {
        if (Context.IsMultiplayer && !Context.IsMainPlayer && !Context.IsSplitScreen)
        {
            if (multi.GetConnectedPlayer(Game1.MasterPlayer.UniqueMultiplayerID)?.GetMod(manifest.UniqueID) is not IMultiplayerPeerMod hostMod)
            {
                monitor.Log(
                    translation.Get("host-not-installed")
                        .Default("The host does not seem to have this mod installed. Some features may not be available"),
                    LogLevel.Warn);
            }
            else if (!hostMod.Version.Equals(manifest.Version))
            {
                monitor.Log(
                    translation.Get("host-version-different")
                        .Default("The host seems to have a different version of this mod ({{version}}). Some features may not work.")
                        .Tokens(new { version = manifest.Version }),
                    LogLevel.Warn);
            }
        }

    }
}