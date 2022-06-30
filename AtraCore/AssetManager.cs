using AtraBase.Collections;
using AtraCore.Models;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace AtraCore;

/// <summary>
/// Handles asset management for this mod.
/// </summary>
internal static class AssetManager
{
    private static readonly string PrismaticMaskData = PathUtilities.NormalizeAssetName("Mods/atravita/DrawPrismaticData");

    internal static List<DrawPrismaticModel>? GetPrismaticModels()
    {
        try
        {
            return Game1.content.Load<List<DrawPrismaticModel>>(PrismaticMaskData);
        }
        catch
        {
            ModEntry.ModMonitor.Log("Failed to load the prismatic mask data!", LogLevel.Error);
        }
        return null;
    }

    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(PrismaticMaskData))
        {
            e.LoadFrom(EmptyContainers.GetEmptyList<DrawPrismaticModel>, AssetLoadPriority.Low);
        }
    }

}
