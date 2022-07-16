using AtraShared.ConstantsAndEnums;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;

namespace TapGiantCrops;

/// <inheritdoc />
internal class ModEntry : Mod
{
    private SObject keg = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.GameLoop.DayEnding += this.OnDayEnding;

        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.keg = new(Vector2.Zero, (int)VanillaMachinesEnum.Keg);
    }
}
