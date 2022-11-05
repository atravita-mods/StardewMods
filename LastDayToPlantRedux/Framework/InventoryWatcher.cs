﻿using AtraBase.Toolkit;
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

    // this isn't perscreen'ed intentionally
    // Probably shouldn't make asset changes different between two players
    // in splitscreen. So in splitscreen it watches both players.
    private static InventoryManagerModel? model = null;

    internal static InventoryManagerModel? Model => model;

    /// <summary>
    /// Gets a value indicating whether whether or not the save model is loaded.
    /// </summary>
    [MemberNotNullWhen(returnValue: true, nameof(model))]
    internal static bool IsModelLoaded => model is not null;

    /// <summary>
    /// Clears the model.
    /// </summary>
    internal static void ClearModel() => model = null;

    internal static bool HasSeedChanges { get; private set; } = true;

    internal static void Reset() => HasSeedChanges = false;

    /*******************************************************************
     * SMAPI complains if there's unicode characters in a save path
     * despite the fact that users can do things like have unicode in their save name.
     * Using a stable hash code instead.
     ******************************************************************/

    /// <summary>
    /// Loads the data model.
    /// </summary>
    /// <param name="helper">SMAPI's data helper.</param>
    [MemberNotNull(nameof(model))]
    internal static void LoadModel(IDataHelper helper)
    {
        model = helper.ReadGlobalData<InventoryManagerModel>($"{SaveString}_{Constants.SaveFolderName!.GetStableHashCode()}") ?? new();
    }

    /// <summary>
    /// Saves the data model.
    /// </summary>
    /// <param name="helper">SMAPI's data helper.</param>
    internal static void SaveModel(IDataHelper helper)
    {
        if (model is not null)
        {
            Task.Run(() => helper.WriteGlobalData($"{SaveString}_{Constants.SaveFolderName!.GetStableHashCode()}", model))
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

    /// <summary>
    /// Watches the inventory.
    /// </summary>
    /// <param name="e">Event args.</param>
    /// <param name="helper">SMAPI's data helper.</param>
    internal static void Watch(InventoryChangedEventArgs e, IDataHelper helper)
    {
        foreach (Item? item in e.Added)
        {
            if (item is SObject obj && !obj.bigCraftable.Value && !obj.isSapling()
                && (obj.Category == SObject.SeedsCategory || obj.Category == SObject.fertilizerCategory))
            {
                if (!IsModelLoaded)
                {
                    LoadModel(helper);
                }
                if (obj.Category == SObject.SeedsCategory && !SObject.isWildTreeSeed(obj.ParentSheetIndex)
                    && !obj.Name.Equals("Mixed Seeds", StringComparison.OrdinalIgnoreCase) && model.Seeds.Add(obj.Name))
                {
                    HasSeedChanges = true;
                }
                else if (obj.Category == SObject.fertilizerCategory && model.Fertilizers.Add(obj.Name))
                {
                    CropAndFertilizerManager.RequestInvalidateFertilizers();
                }
            }
        }
    }
}
