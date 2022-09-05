using StardewModdingAPI.Events;

namespace LastDayToPlantRedux.Framework;

/// <summary>
/// Model to save for inventory data.
/// </summary>
public sealed class InventoryManagerModel
{
    public HashSet<string> Seeds { get; set; } = new();
}

/// <summary>
/// Watches the player inventory to see if seeds enter it.
/// </summary>
internal static class InventoryWatcher
{
    private const string savestring = "InventoryModel";

    private static InventoryManagerModel? model = null;

    internal static bool HasChanges { get; set; } = false;

    [MemberNotNullWhen(returnValue: true, nameof(model))]
    internal static bool IsModelLoaded => model is not null;

    internal static void ClearModel() => model = null;

    [MemberNotNull(nameof(model))]
    internal static void LoadModel(IDataHelper helper)
    {
        model = helper.ReadGlobalData<InventoryManagerModel>($"{savestring}_{Constants.SaveFolderName!.GetHashCode()}") ?? new();
    }

    internal static void SaveModel(IDataHelper helper)
    {
        if (model is not null)
        {
            Task.Run(() => helper.WriteGlobalData($"{savestring}_{Constants.SaveFolderName!.GetHashCode()}", model))
                .ContinueWith(t =>
                {
                    switch (t.Status)
                    {
                        case TaskStatus.RanToCompletion:
                            ModEntry.ModMonitor.Log("Data model written successfully!");
                            break;
                        case TaskStatus.Faulted:
                            ModEntry.ModMonitor.Log($"Data model failed to write {t.Exception}", LogLevel.Error);
                            break;
                    }
                });
        }
    }

    internal static void Watch(InventoryChangedEventArgs e, IDataHelper helper)
    {
        foreach (var item in e.Added)
        {
            if (item is SObject obj && !obj.bigCraftable.Value && obj.Category == SObject.SeedsCategory)
            {
                if (!IsModelLoaded)
                {
                    LoadModel(helper);
                }
                if (model.Seeds.Add(obj.Name))
                {
                    HasChanges = true;
                }
            }
        }
    }
}
