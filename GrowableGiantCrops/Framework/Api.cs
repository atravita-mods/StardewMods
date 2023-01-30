namespace GrowableGiantCrops.Framework;

/// <summary>
/// The API for this mod.
/// </summary>
public sealed class Api : IGrowableGiantCropsAPI
{
    public bool IsShovel(Tool tool) => tool is ShovelTool;
}
