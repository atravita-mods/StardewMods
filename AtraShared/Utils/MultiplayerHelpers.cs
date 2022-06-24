using AtraBase.Toolkit.Reflection;

namespace AtraShared.Utils;

/// <summary>
/// Functions to help with handling multiplayer.
/// </summary>
internal static class MultiplayerHelpers
{
    private static readonly Lazy<Func<Multiplayer>> MultiplayerLazy = new(() => typeof(Game1).StaticFieldNamed("multiplayer").GetStaticFieldGetter<Multiplayer>());

    /// <summary>
    /// Gets a function that returns the current multiplayer instance.
    /// </summary>
    internal static Func<Multiplayer> GetMultiplayer => MultiplayerLazy.Value;

    /// <summary>
    /// Checks if the versions installed of the mod are the same for farmhands.
    /// Prints errors to console if wrong.
    /// </summary>
    /// <param name="multi">Multiplayer helper.</param>
    /// <param name="manifest">Manifest of mod.</param>
    /// <param name="monitor">Logger.</param>
    /// <param name="translation">Translation helper.</param>
    internal static void AssertMultiplayerVersions(IMultiplayerHelper multi, IManifest manifest, IMonitor monitor, ITranslationHelper translation)
    {
        if (Context.IsMultiplayer && !Context.IsMainPlayer && !Context.IsSplitScreen)
        {
            if (multi.GetConnectedPlayer(Game1.MasterPlayer.UniqueMultiplayerID)?.GetMod(manifest.UniqueID) is not IMultiplayerPeerMod hostMod)
            {
                monitor.Log(
                    translation.Get("host-not-installed")
                        .Default("The host does not seem to have this mod installed. Some features may not be available."),
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