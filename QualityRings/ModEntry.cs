using AtraCore.Framework.Internal;

using AtraShared.Integrations;
using SpaceCore;
using StardewModdingAPI.Events;

namespace QualityRings;

/// <inheritdoc />
internal sealed class ModEntry : BaseMod<ModEntry>
{
    private static IApi? spacecoreAPI;

    internal static IApi? SpaceCoreAPI => spacecoreAPI;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        base.Entry(helper);
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        IntegrationHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry);
        if (!helper.TryGetAPI("spacechase0.SpaceCore", "1.5.10", out spacecoreAPI))
        {
            this.Monitor.Log($"Spacecore could not be found, this mod will not work.", LogLevel.Error);
        }
    }
}
