using AtraBase.Toolkit.Extensions;

using AtraShared.Wrappers;

using StardewModdingAPI.Events;

namespace FarmCaveSpawn;

public sealed class InventoryManagerModel
{
    public HashSet<string> Saplings { get; set; } = new();
}

internal static class InventoryWatcher
{
    private const string SaveString = "InventoryModel";
    private const string DATAPACKAGE = "DATAPACKAGE";
    private const string SINGLE = "SINGLE";

    private static InventoryManagerModel? model;
    private static string UniqueID = null!;

    internal static void Initialize(string uniqueID) => UniqueID = uniqueID;

    internal static void Load(IMultiplayerHelper multi, IDataHelper data)
    {
        if (Context.IsMainPlayer)
        {
            model = data.ReadSaveData<InventoryManagerModel>(SaveString) ?? new();

            multi.SendMessage(
                message: model,
                messageType: DATAPACKAGE,
                modIDs: new[] { UniqueID },
                playerIDs: multi.GetConnectedPlayers().Where(p => !p.IsSplitScreen).Select(p => p.PlayerID).ToArray());
        }
    }

    internal static void Watch(InventoryChangedEventArgs e, IMultiplayerHelper multi)
    {
        foreach (var item in e.Added)
        {
            if (item is SObject obj && obj.isSapling() && Game1Wrappers.ObjectInfo.TryGetValue(obj.ParentSheetIndex, out string? data))
            {
                string name = data.GetNthChunk('/').ToString();
                if (model?.Saplings?.Add(name) == true)
                {
                    multi.SendMessage(name, SINGLE, new[] { UniqueID });
                    ModEntry.RequestFruitListReset();
                }
            }
        }
    }

    internal static bool HaveSeen(int parentSheetIndex)
    {
        if (Game1Wrappers.ObjectInfo.TryGetValue(parentSheetIndex, out string? data))
        {
            string name = data.GetNthChunk('/').ToString();
            return model?.Saplings?.Contains(name) == true;
        }
        return false;
    }

    internal static void OnPeerConnected(PeerConnectedEventArgs e, IMultiplayerHelper multi)
    {
        if (Context.IsMainPlayer && model is not null)
        {
           multi.SendMessage(
                message: model,
                messageType: DATAPACKAGE,
                modIDs: new[] { UniqueID },
                playerIDs: new[] { e.Peer.PlayerID });
        }
    }

    internal static void OnModMessageRecieved(ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != UniqueID || Context.ScreenId != 0)
        {
            return;
        }

        switch (e.Type)
        {
            case DATAPACKAGE:
            {
                model = e.ReadAs<InventoryManagerModel>();
                break;
            }
            case SINGLE:
            {
                string name = e.ReadAs<string>();
                if (model?.Saplings?.Add(name) == true)
                {
                    ModEntry.RequestFruitListReset();
                }
                break;
            }
        }
    }
}
