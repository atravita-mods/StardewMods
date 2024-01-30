#nullable enable

namespace ExperimentalLagReduction;

/// <summary>
/// The API for this mod.
/// </summary>
public interface IExperimentalLagReductionAPI
{
    /// <summary>
    /// Given two game locations, tries to find the best path between them.
    /// </summary>
    /// <param name="start">Starting location.</param>
    /// <param name="end">Ending location.</param>
    /// <param name="gender">The gender of the NPC to consider. Valid values are <c>NPC.male</c>, <c>NPC.female</c>, and <c>NPC.undefined</c> (for no gender constraints).</param>
    /// <param name="allowPartialPaths">Whether or not to allow partial path piecing. Partial piecing is faster, but may cause NPCs to use slightly longer paths.</param>
    /// <returns>The map-to-map travel list, or null if not possible.</returns>
    public string[]? GetPathFor(GameLocation start, GameLocation end, Gender gender, bool allowPartialPaths);

    /// <summary>
    /// Asks the pathfinder cache to clear nulls only (ie, paths that cannot be reached.)
    /// </summary>
    /// <returns>True if values were removed, false otherwise.</returns>
    public bool ClearPathNulls();

    /// <summary>
    /// Clears the pathfinding cache, if it has values in it.
    /// </summary>
    /// <returns>True if cleared, false otherwise.</returns>
    public bool ClearMacroCache();

    /// <summary>
    /// Asks the pathfinding cache to pre-populate itself.
    /// </summary>
    /// <param name="parallel">Whether or not to try running off the main thread.</param>
    /// <returns>true if populated, false otherwise.</returns>
    public bool PrePopulateCache(bool parallel);

    /// <summary>
    /// Forcibly clears the gift cache.
    /// </summary>
    public void ResetGiftCache();
}
