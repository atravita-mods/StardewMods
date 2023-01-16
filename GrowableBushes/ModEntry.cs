using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;

using GrowableBushes.Framework;

using StardewModdingAPI.Events;

namespace GrowableBushes;

// TODO:
// * Placement code for bushes.
// * Override all the necessary draw methods.
// * Make sure you can axe a bush (in case you want to move it).
// * Bushes for sale.
// * Smart Building compat.

/// <inheritdoc />
internal sealed class ModEntry : Mod
{

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        IntegrationHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, LogLevel.Error);

        if (helper.TryGetAPI("spacechase0.SpaceCore", "1.9.3", out ICompleteSpaceCoreAPI? api))
        {
            api.RegisterSerializerType(typeof(InventoryBush));

            this.Helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }
        else
        {
            this.Monitor.Log($"Could not load spacecore's API. This is a fatal error.", LogLevel.Error);
        }
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (Context.IsPlayerFree && e.Button == SButton.L)
        {
            InventoryBush bush = new(0, 1);
            Game1.player.addItemByMenuIfNecessaryElseHoldUp(bush);
        }
    }
}