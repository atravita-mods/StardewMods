using AtraShared.Utils.Extensions;

using MiniAtraShared.Extensions;

namespace GingerIslandMainlandAdjustments.AssetManagers;

/// <summary>
/// Watches IslandSouth's resort parrot.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="IslandSouthWatcher"/> class.
/// </remarks>
/// <param name="contentHelper">SMAPI's game content helper.</param>
internal sealed class IslandSouthWatcher(IGameContentHelper contentHelper)
{

    /// <summary>
    /// Called when the resort is fixed.
    /// </summary>
    internal void OnResortFixed()
    {
        Globals.ModMonitor.DebugOnlyLog("Resort fixed! Invalidating.", LogLevel.Info);

        try
        {
            foreach (string name in AssetEditor.CharacterDialogues)
            {
                contentHelper.InvalidateCacheAndLocalized(AssetEditor.Dialogue + name);
            }
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.LogError("refreshing NPC dialogue", ex);
        }
    }
}
