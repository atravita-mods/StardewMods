using AtraCore.Framework.Internal;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using ShopTabs.Framework;

using StardewModdingAPI.Events;

/// <inheritdoc/>
internal sealed class ModEntry : BaseMod<ModEntry>
{
    internal static IGameContentHelper GameContentHelper { get; private set; } = null!;

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        base.Entry(helper);
        GameContentHelper = helper.GameContent;
        AssetEditor.Init(helper.GameContent);
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        helper.Events.Content.AssetRequested += static (_, e) => AssetEditor.Edit(e);
        helper.Events.Content.AssetsInvalidated += static (_, e) => AssetEditor.Refresh(e.NamesWithoutLocale);
        helper.Events.Player.Warped += static (_, e) => AssetEditor.Refresh();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        GameContentHelper = null!;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.ApplyPatches(new(this.ModManifest.UniqueID));
        ShopTabsManager.Init();
        this.Helper.Events.Display.MenuChanged += static (_, e) => ShopTabsManager.OnShopMenu(e);
    }

    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.PatchAll(typeof(ModEntry).Assembly);
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }

        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }
}
