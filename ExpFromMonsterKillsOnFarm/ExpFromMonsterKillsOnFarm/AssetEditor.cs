

using StardewModdingAPI.Events;

using StardewValley.GameData.SpecialOrders;

namespace ExpFromMonsterKillsOnFarm;

/// <summary>
/// Edits assets for this mod.
/// </summary>
internal static class AssetEditor
{
    private static IAssetName SpecialOrders = null!;

    internal static void Init(IGameContentHelper parser)
    {
        SpecialOrders = parser.ParseAssetName("Data/SpecialOrders");
    }

    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(SpecialOrders))
        {
            e.Edit(SpecialOrderEditor, AssetEditPriority.Late);
        }
    }

    private static void SpecialOrderEditor(IAssetData asset)
    {
        var data = asset.AsDictionary<string, SpecialOrderData>().Data;

        foreach (var item in data.Values)
        {
            if (item.Objectives?.Count is 0 or null)
            {
                continue;
            }

            foreach (SpecialOrderObjectiveData? objective in item.Objectives)
            {
                if (objective.Type == "Slay")
                {
                    objective.Data["IgnoreFarmMonsters"] = "false";
                }
            }
        }
    }
}
