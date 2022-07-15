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
        this.keg = new(Vector2.Zero, (int)VanillaMachinesEnum.Keg);

        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.GameLoop.DayEnding += this.OnDayEnding;
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e) => throw new NotImplementedException();

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e) => throw new NotImplementedException();
}
