namespace GrowableGiantCrops.Framework;

/// <summary>
/// The API for this mod.
/// </summary>
public sealed class Api : IGrowableGiantCropsAPI
{
    /// <inheritdoc />
    public bool IsShovel(Tool tool) => tool is ShovelTool;

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

    #endregion
}
