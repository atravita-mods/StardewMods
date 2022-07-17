using AtraShared.ConstantsAndEnums;
using AtraShared.Menuing;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using TapGiantCrops.Framework;

namespace TapGiantCrops;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private SObject keg = null!;
    private TapGiantCrop api = new();

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.GameLoop.DayEnding += this.OnDayEnding;

        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
    }

    /// <inheritdoc />
    public override object? GetApi() => this.api;

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!MenuingExtensions.IsNormalGameplay())
        {
            return;
        }
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.keg = new(Vector2.Zero, (int)VanillaMachinesEnum.Keg);
    }
}
