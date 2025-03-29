using HarmonyLib;

using MiniAtraShared.Models;

using SlightlyMoreDehardcoding.EventCommands;
using SlightlyMoreDehardcoding.Framework;
using SlightlyMoreDehardcoding.HarmonyPatches;

using StardewModdingAPI.Events;

namespace SlightlyMoreDehardcoding;

/// <inheritdoc />
internal sealed class ModEntry : BaseMod<ModEntry>
{
    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        base.Entry(helper);

        AssetManager.Init(helper.GameContent, helper.ModRegistry.ModID);
        helper.Events.Content.AssetRequested += static (_, e) => AssetManager.Apply(e);

        Harmony harmony = new(helper.ModRegistry.ModID);

        LetterMenuPatch.Apply(harmony);
        PlayerControl.Apply(harmony);
        ObjectivePatch.Apply(harmony);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        Event.RegisterCommand("atravita_" + nameof(MoveTo), MoveTo.Command);

        Event.RegisterCommand("atravita_" + nameof(BranchIf), BranchIf.BranchIfCommand);

        Event.RegisterCommand("atravita_" + nameof(FestivalCommands.SetFestivalHost), FestivalCommands.SetFestivalHost);

        Event.RegisterCommand("atravita_" + nameof(PlayerControl), PlayerControl.Command);
        Event.RegisterCommand("atravita_" + nameof(PlayerControl.AddActionForTile), PlayerControl.AddActionForTile);
        Event.RegisterCommand("atravita_" + nameof(PlayerControl.AddEventCommandForTile), PlayerControl.AddEventCommandForTile);
        Event.RegisterCommand("atravita_" + nameof(PlayerControl.RemoveActionForTile), PlayerControl.RemoveActionForTile);
        Event.RegisterCommand("atravita_" + nameof(PlayerControl.RemoveActionForAllTiles), PlayerControl.RemoveActionForAllTiles);
    }

}
