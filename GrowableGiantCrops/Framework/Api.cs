﻿using AtraShared.Utils.Extensions;

using GrowableGiantCrops.Framework.InventoryModels;

using Microsoft.Xna.Framework;

using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops.Framework;

/// <summary>
/// The API for this mod.
/// </summary>
public sealed class Api : IGrowableGiantCropsAPI
{
    #region shovel

    /// <inheritdoc />
    public bool IsShovel(Tool tool) => tool is ShovelTool;

    /// <inheritdoc />
    public Tool GetShovel() => new ShovelTool();

    #endregion

    #region any

    /// <inheritdoc/>
    public bool CanPlace(SObject obj, GameLocation loc, Vector2 tile, bool relaxed)
    => obj switch
        {
            InventoryResourceClump clump => this.CanPlaceClump(clump, loc, tile, relaxed),
            _ => false,
        };

    /// <inheritdoc/>
    public bool TryPlace(SObject obj, GameLocation loc, Vector2 tile, bool relaxed)
    => obj switch
        {
            InventoryResourceClump clump => this.TryPlaceClump(clump, loc, tile, relaxed),
            _ => false,
        };

    /// <inheritdoc/>
    public bool CanPickUp(GameLocation loc, Vector2 tile, bool placedOnly = false)
    => loc.GetLargeObjectAtLocation(((int)tile.X * Game1.tileSize) + 32, ((int)tile.Y * Game1.tileSize) + 32, placedOnly) switch
    {
        GiantCrop crop => this.CanPickUpCrop(crop, placedOnly) is not null,
        ResourceClump clump => this.CanPickUpClump(clump, placedOnly) != ResourceClumpIndexes.Invalid,
        _ => false
    };

    /// <inheritdoc/>
    public SObject? TryPickUp(GameLocation loc, Vector2 tile, bool placedOnly = false)
    {
        if (this.TryPickUpClump(loc, tile, placedOnly) is SObject @object)
        {
            return @object;
        }
        return null;
    }

    /// <inheritdoc />
    public void DrawPickUpGraphics(SObject obj, GameLocation loc, Vector2 tile)
    {
        switch (obj)
        {
            case InventoryGiantCrop crop:
                ShovelTool.AddAnimations(loc, tile, crop.TexturePath, crop.SourceRect, crop.TileSize);
                break;
            case InventoryResourceClump clump:
                ShovelTool.AddAnimations(loc, tile, Game1.objectSpriteSheetName, clump.SourceRect, new Point(2, 2));
                break;
            case SObject @object when @object.bigCraftable.Value:
                ShovelTool.AddAnimations(loc, tile, Game1.objectSpriteSheetName, GameLocation.getSourceRectForObject(@object.ParentSheetIndex), new Point(1, 1));
                break;
        }
    }

    /// <inheritdoc />
    public void DrawAnimations(GameLocation loc, Vector2 tile, string? texturePath, Rectangle sourceRect, Point tileSize)
        => ShovelTool.AddAnimations(loc, tile, texturePath, sourceRect, tileSize);

    #endregion

    #region clumps

    /// <inheritdoc />
    public ResourceClumpIndexes GetIndexOfClumpIfApplicable(SObject obj)
    {
        if (obj is InventoryResourceClump clump)
        {
            ResourceClumpIndexes idx = (ResourceClumpIndexes)clump.ParentSheetIndex;
            if (ResourceClumpIndexesExtensions.IsDefined(idx))
            {
                return idx;
            }
        }
        return ResourceClumpIndexes.Invalid;
    }

    /// <inheritdoc />
    public SObject GetResourceClump(ResourceClumpIndexes idx) => new InventoryResourceClump(idx, 1);

    /// <inheritdoc />
    public bool CanPlaceClump(SObject obj, GameLocation loc, Vector2 tile, bool relaxed)
        => obj is InventoryResourceClump clump && clump.CanPlace(loc, tile, relaxed);

    /// <inheritdoc />
    public bool TryPlaceClump(SObject obj, GameLocation loc, Vector2 tile, bool relaxed)
        => obj is InventoryResourceClump clump && clump.PlaceResourceClump(loc, (int)tile.X * Game1.tileSize, (int)tile.Y * Game1.tileSize, relaxed);

    /// <inheritdoc />
    public ResourceClumpIndexes CanPickUpClump(GameLocation loc, Vector2 tile, bool placedOnly = false)
        => loc.GetLargeObjectAtLocation(((int)tile.X * Game1.tileSize) + 32, ((int)tile.Y * Game1.tileSize) + 32, placedOnly) switch
            {
                ResourceClump clump => this.CanPickUpClump(clump, placedOnly),
                _ => ResourceClumpIndexes.Invalid
            };

    /// <inheritdoc />
    public ResourceClumpIndexes CanPickUpClump(ResourceClump clump, bool placedOnly = false)
    {
        if (clump.GetType() != typeof(ResourceClump))
        {
            return ResourceClumpIndexes.Invalid;
        }
        if (!placedOnly && ResourceClumpIndexesExtensions.IsDefined((ResourceClumpIndexes)clump.parentSheetIndex.Value))
        {
            return (ResourceClumpIndexes)clump.parentSheetIndex.Value;
        }
        return clump.modData?.GetEnum(InventoryResourceClump.ResourceModdata, ResourceClumpIndexes.Invalid) ?? ResourceClumpIndexes.Invalid;
    }

    /// <inheritdoc />
    public SObject? TryPickUpClump(GameLocation loc, Vector2 tile, bool placedOnly = false)
    =>  loc.RemoveLargeObjectAtLocation(((int)tile.X * Game1.tileSize) + 32, ((int)tile.Y * Game1.tileSize) + 32, placedOnly) switch
        {
            GiantCrop giant => this.GetMatchingCrop(giant),
            ResourceClump resource => this.GetMatchingClump(resource),
            _ => null,
        };

    /// <inheritdoc />
    public SObject? GetMatchingClump(ResourceClump resource)
    {
        if (resource is GiantCrop crop)
        {
            return this.GetMatchingCrop(crop);
        }

        ResourceClumpIndexes idx = (ResourceClumpIndexes)resource.parentSheetIndex.Value;
        if (idx != ResourceClumpIndexes.Invalid && ResourceClumpIndexesExtensions.IsDefined(idx))
        {
            return new InventoryResourceClump(idx, 1) { TileLocation = resource.tile.Value };
        }
        return null;
    }

    #endregion

    #region crops

    /// <inheritdoc />
    public (int idx, string? stringId)? GetIdentifiers(SObject obj)
    {
        if (obj is InventoryGiantCrop crop)
        {
            string? stringID = string.IsNullOrEmpty(crop.stringID.Value) ? null : crop.stringID.Value;
            return (crop.ParentSheetIndex, stringID);
        }
        return null;
    }

    /// <inheritdoc />
    public SObject? GetGiantCrop(int produceIndex, int initialStack)
    => InventoryGiantCrop.IsValidGiantCropIndex(produceIndex)
        ? new InventoryGiantCrop(produceIndex, initialStack)
        : null;

    /// <inheritdoc />
    public SObject? GetGiantCrop(string stringID, int produceIndex, int initialStack)
    => ModEntry.GiantCropTweaksAPI?.GiantCrops?.ContainsKey(stringID) == true
        ? new InventoryGiantCrop(stringID, produceIndex, initialStack)
        : null;

    /// <inheritdoc />
    public SObject? GetMatchingCrop(GiantCrop giant)
    {
        InventoryGiantCrop? inventoryGiantCrop = null;
        if (giant.modData.TryGetValue(InventoryGiantCrop.GiantCropTweaksModDataKey, out string? stringID)
            && ModEntry.GiantCropTweaksAPI?.GiantCrops.ContainsKey(stringID) == true)
        {
            inventoryGiantCrop = new InventoryGiantCrop(stringID, giant.parentSheetIndex.Value, 1);
        }
        else if (InventoryGiantCrop.IsValidGiantCropIndex(giant.parentSheetIndex.Value))
        {
            inventoryGiantCrop = new InventoryGiantCrop(giant.parentSheetIndex.Value, 1);
        }

        if (inventoryGiantCrop is not null)
        {
            inventoryGiantCrop.TileLocation = giant.tile.Value;
            return inventoryGiantCrop;
        }
        return null;
    }

    /// <inheritdoc />
    public (int idx, string? stringId)? CanPickUpCrop(GiantCrop crop, bool placedOnly)
    {
        if (!placedOnly || crop.modData?.ContainsKey(InventoryGiantCrop.ModDataKey) == true)
        {
            if (crop.modData?.TryGetValue(InventoryGiantCrop.GiantCropTweaksModDataKey, out var stringID) == true
                && !string.IsNullOrEmpty(stringID))
            {
                return (crop.parentSheetIndex.Value, stringID);
            }
            else if (InventoryGiantCrop.IsValidGiantCropIndex(crop.parentSheetIndex.Value))
            {
                return (crop.parentSheetIndex.Value, null);
            }
        }
        return null;
    }

    public (int idx, string? stringId)? CanPickUpCrop(GameLocation loc, Vector2 tile, bool placedOnly)
        => loc.GetLargeObjectAtLocation(((int)tile.X * Game1.tileSize) + 32, ((int)tile.Y * Game1.tileSize) + 32, placedOnly) switch
        {
            GiantCrop crop => this.CanPickUpClump(crop, placedOnly),
            _ => null,
        };

    #endregion
}
