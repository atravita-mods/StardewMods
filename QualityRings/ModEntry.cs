﻿using AtraShared.Integrations;
using SpaceCore;
using StardewModdingAPI.Events;

namespace QualityRings;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private static IApi? spacecoreAPI;

    internal static IApi? SpaceCoreAPI => spacecoreAPI;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        this.Monitor.Log($"Starting up: {this.ModManifest.UniqueID} - {typeof(ModEntry).Assembly.FullName}");
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
