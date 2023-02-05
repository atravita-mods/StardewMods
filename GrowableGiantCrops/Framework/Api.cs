using GrowableGiantCrops.Framework.InventoryModels;

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
