﻿using System;
using AtraShared.Utils.Extensions;
using Microsoft.Xna.Framework;
using Pathoschild.Stardew.Automate;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;

namespace TapGiantCropsAutomateBridge.Framework;


/// <summary>
/// The automation factory for tapped giant crops.
/// </summary>
public class TappedGiantCropFactory : IAutomationFactory
{
    /// <inheritdoc />
    public IAutomatable? GetFor(SObject obj, GameLocation location, in Vector2 tile)
    {
        if (obj.Name.Contains("Tapper", StringComparison.OrdinalIgnoreCase))
        {
            foreach (ResourceClump? feature in location.resourceClumps)
            {
                if (feature is GiantCrop crop)
                {
                    Vector2 offset = feature.tile.Value;
                    offset.Y += crop.height.Value - 1;
                    offset.X += crop.width.Value / 2;
                    if (tile == offset)
                    {
                        AutomateBridge.ModMonitor.DebugOnlyLog($"Attempting to add automateable giant crop at {location.NameOrUniqueName} - {crop.tile}", LogLevel.Info);
                        return new TappedGiantCrop(crop, obj, location, new Rectangle((int)crop.tile.X, (int)crop.tile.Y, crop.width.Value, crop.height.Value));
                    }
                }
            }
        }
        return null;
    }

    /// <inheritdoc />
    public IAutomatable? GetFor(TerrainFeature feature, GameLocation location, in Vector2 tile) => null;

    /// <inheritdoc />
    public IAutomatable? GetFor(Building building, BuildableGameLocation location, in Vector2 tile) => null;

    /// <inheritdoc />
    public IAutomatable? GetForTile(GameLocation location, in Vector2 tile) => null;
}
