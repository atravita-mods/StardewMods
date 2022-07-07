using AtraBase.Collections;
using AtraCore.Models;
using StardewModdingAPI.Events;

namespace AtraCore;

/// <summary>
/// Handles asset management for this mod.
/// </summary>
internal static class AssetManager
{
    internal static Dictionary<int, DrawPrismaticModel>? GetPrismaticModels()
    {
        try
        {
            return Game1.content.Load<Dictionary<int, DrawPrismaticModel>>(AtraCoreConstants.PrismaticMaskData);
        }
        catch
        {
            ModEntry.ModMonitor.Log("Failed to load the prismatic mask data!", LogLevel.Error);
        }
        return null;
    }

    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(AtraCoreConstants.PrismaticMaskData))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<int, DrawPrismaticModel>, AssetLoadPriority.Low);
        }
    }

}
