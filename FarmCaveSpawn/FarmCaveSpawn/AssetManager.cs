﻿using AtraBase.Collections;

using StardewModdingAPI.Events;

namespace FarmCaveSpawn;

/// <summary>
/// Handles the fake assets for this mod.
/// </summary>
internal static class AssetManager
{
    /// <summary>
    /// Gets fake asset location for the denylist.
    /// </summary>
    internal static IAssetName DENYLIST_LOCATION { get; private set; } = null!;

    /// <summary>
    /// Gets fake asset location for more locations that can spawn in fruit.
    /// </summary>
    internal static IAssetName ADDITIONAL_LOCATIONS_LOCATION { get; private set; } = null!;

    /// <summary>
    /// Initialize the AssetManager.
    /// </summary>
    /// <param name="parser">Game Content Helper.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        DENYLIST_LOCATION = parser.ParseAssetName("Mods/atravita_FarmCaveSpawn_denylist");
        ADDITIONAL_LOCATIONS_LOCATION = parser.ParseAssetName("Mods/atravita_FarmCaveSpawn_additionalLocations");
    }

    /// <summary>
    /// Loads assets for this mod.
    /// </summary>
    /// <param name="e">Event args.</param>
    internal static void Load(AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo(DENYLIST_LOCATION))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<string, string>, AssetLoadPriority.Low);
        }
        else if (e.Name.IsEquivalentTo(ADDITIONAL_LOCATIONS_LOCATION))
        {
            e.LoadFrom(GetInitialAdditionalLocations, AssetLoadPriority.High);
        }
    }

    private static Dictionary<string, string> GetInitialAdditionalLocations()
        => new()
        {
            ["FlashShifter.SVECode"] = "Custom_MinecartCave, Custom_DeepCave",
#if DEBUG // Regex's test!
            ["atravita.FarmCaveSpawn"] = "Town:[(4;5);(34;40)]",
#endif
        };
}