using StardewModdingAPI.Events;

namespace LastDayToPlantRedux.Framework;

/// <summary>
/// Model to save for inventory data.
/// </summary>
public sealed class InventoryManagerModel
{
    /// <summary>
    /// Gets or sets a hashet of seeds the player has seen before.
    /// </summary>
    public HashSet<string> Seeds { get; set; } = new();

    /// <summary>
    /// Gets or sets a hashset of fertilizers the player has seen before.
    /// </summary>
    public HashSet<string> Fertilizers { get; set; } = new();
}

/// <summary>
/// Watches the player inventory to see if seeds or fertilizer enter it.
/// </summary>
internal static class InventoryWatcher
{
    private const string SaveString = "InventoryModel";

    private static InventoryManagerModel? model = null;

    internal static bool HasChanges { get; set; } = false;

    [MemberNotNullWhen(returnValue: true, nameof(model))]
    internal static bool IsModelLoaded => model is not null;

    internal static void ClearModel() => model = null;

    [MemberNotNull(nameof(model))]
    internal static void LoadModel(IDataHelper helper)
    {
        model = helper.ReadGlobalData<InventoryManagerModel>($"{SaveString}_{Constants.SaveFolderName!.GetHashCode()}") ?? new();
    }

    internal static void SaveModel(IDataHelper helper)
    {
        if (model is not null)
        {
            Task.Run(() => helper.WriteGlobalData($"{SaveString}_{Constants.SaveFolderName!.GetHashCode()}", model))
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
            if (item is SObject obj && !obj.bigCraftable.Value
                && (obj.Category == SObject.SeedsCategory || obj.Category == SObject.fertilizerCategory))
            {
                if (!IsModelLoaded)
                {
                    LoadModel(helper);
                }
                if (obj.Category == SObject.SeedsCategory && model.Seeds.Add(obj.Name))
                {
                    HasChanges = true;
                }
                else if (obj.Category == SObject.fertilizerCategory && model.Fertilizers.Add(obj.Name))
                {
                    HasChanges = true;
                }
            }
        }
    }
}
