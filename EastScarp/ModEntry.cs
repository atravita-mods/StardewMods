namespace EastScarp;

using EastScarp.Framework;
using EastScarp.Models;

using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private readonly PerScreen<Model?> _activeModel = new();

    /// <summary>Gets the logger for this mod.</summary>
    internal static IMonitor ModMonitor { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        ModMonitor = this.Monitor;

        AssetManager.Init(helper.GameContent);
        helper.Events.Content.AssetRequested += static (_, e) => AssetManager.Apply(e);
        helper.Events.Content.AssetsInvalidated += static (_, e) => AssetManager.Invalidate(e.NamesWithoutLocale);

        helper.Events.Player.Warped += this.OnPlayerWarped;
        helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
        helper.Events.GameLoop.OneSecondUpdateTicked += this.OnSecondTicked;
        helper.Events.GameLoop.UpdateTicked += this.OnTicked;
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.eventUp)
        {
            return;
        }

        if (this._activeModel.Value is not { } model || Game1.currentLocation is not { } location || Game1.player is not { } player)
        {
            return;
        }

        SpawnCrittersAndMonsters(model, SpawnTrigger.OnTimeChange, location, player);
    }

    private void OnTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.eventUp)
        {
            return;
        }

        if (this._activeModel.Value is not { } model || Game1.currentLocation is not { } location || Game1.player is not { } player)
        {
            return;
        }

        SpawnCrittersAndMonsters(model, SpawnTrigger.OnTick, location, player);
    }

    private void OnSecondTicked(object? sender, OneSecondUpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.eventUp)
        {
            return;
        }

        if (this._activeModel.Value is not { } model || Game1.currentLocation is not { } location || Game1.player is not { } player)
        {
            return;
        }

        SpawnCrittersAndMonsters(model, SpawnTrigger.OnSecond, location, player);
    }

    /// <inheritdoc cref="IPlayerEvents.Warped"/>
    private void OnPlayerWarped(object? sender, WarpedEventArgs e)
    {
        // it's possible for this event to be raised for a "false warp".
        if (!e.IsLocalPlayer || ReferenceEquals(e.NewLocation, e.OldLocation))
        {
            return;
        }

        this.Monitor.VerboseLog($"Switching from {e.OldLocation?.Name ?? "null"} to {e.NewLocation?.Name ?? "null" }");

        if (e.NewLocation is null || !AssetManager.Data.TryGetValue(e.NewLocation.Name, out Model? data))
        {
            this._activeModel.Value = null;
            return;
        }

        this._activeModel.Value = data;

        // check for valid water color.
        foreach (var color in data.WaterColor)
        {
            if (color.CheckCondition(e.NewLocation, e.Player))
            {
                var c = Utility.StringToColor(color.Color);
                if (c is not null)
                {
                    this.Monitor.VerboseLog($"Assigning {c.Value} to water color at {e.NewLocation}");
                    e.NewLocation.waterColor.Value = c.Value;
                }
            }
        }

        SpawnCrittersAndMonsters(data, SpawnTrigger.OnEntry, e.NewLocation, e.Player);
    }

    private static void SpawnCrittersAndMonsters(Model data, SpawnTrigger trigger, GameLocation location, Farmer player)
    {
        // check for monster spawn, skip if there's one already.
        if (data.SeaMonsterSpawn.Count > 0 && !location.temporarySprites.Any(static s => s is SeaMonsterTemporarySprite))
        {
            foreach (var monsterSpawn in data.SeaMonsterSpawn)
            {
                SeaMonsterSpawner.SpawnMonster(monsterSpawn, trigger, location, player);
            }
        }
    }
}
