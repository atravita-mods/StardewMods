using AtraShared.ConstantsAndEnums;
using AtraShared.Menuing;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;

namespace TapGiantCrops;

/// <inheritdoc />
internal class ModEntry : Mod, ITapGiantCropsAPI
{
    private SObject keg = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.GameLoop.DayEnding += this.OnDayEnding;

        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
    }

    /// <inheritdoc />
    public override object? GetApi() => this;

    /// <inheritdoc />
    public bool CanPlaceTapper(GameLocation loc, Vector2 tile) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool TryPlaceTapper(GameLocation loc, Vector2 tile) => throw new NotImplementedException();

    /// <inheritdoc />
    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!MenuingExtensions.CanRaiseMenu())
        {
            return;
        }
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.keg = new(Vector2.Zero, (int)VanillaMachinesEnum.Keg);
    }
}
