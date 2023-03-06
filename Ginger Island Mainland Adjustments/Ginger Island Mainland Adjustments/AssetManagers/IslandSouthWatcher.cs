using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AtraShared.Utils.Extensions;

using Netcode;

namespace GingerIslandMainlandAdjustments.AssetManagers;
internal sealed class IslandSouthWatcher
{
    private IGameContentHelper _contentHelper;

    public IslandSouthWatcher(IGameContentHelper contentHelper) => this._contentHelper = contentHelper;

    internal void OnResortFixed()
    {
        Globals.ModMonitor.DebugOnlyLog("Resort fixed! Invalidating.", LogLevel.Info);

        foreach (var name in AssetEditor.CharacterDialogues)
        {
            this._contentHelper.InvalidateCacheAndLocalized(AssetEditor.Dialogue + name);
        }
    }
}
