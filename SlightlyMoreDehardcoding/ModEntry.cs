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

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        AssetManager.Init(helper.GameContent, helper.ModRegistry.ModID);
        helper.Events.Content.AssetRequested += static (_, e) => AssetManager.Apply(e);

        Harmony harmony = new(helper.ModRegistry.ModID);

        LetterMenuPatch.Apply(harmony);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        Event.RegisterCommand("atravita_" + nameof(BranchIf), BranchIf.BranchIfCommand);
        Event.RegisterCommand("atravita_" + nameof(FestivalCommands.SetFestivalHost), FestivalCommands.SetFestivalHost);
        Event.RegisterCommand("atravita_" + nameof(PlayerControl), PlayerControl.Command);
    }

}
