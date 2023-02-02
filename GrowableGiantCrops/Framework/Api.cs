namespace GrowableGiantCrops.Framework;

/// <summary>
/// The API for this mod.
/// </summary>
public sealed class Api : IGrowableGiantCropsAPI
{
    /// <inheritdoc />
    public SObject GetResourceClump(ResourceClumpIndexes idx) => new InventoryResourceClump(idx, 1);

    /// <inheritdoc />
    public bool IsShovel(Tool tool) => tool is ShovelTool;
}
