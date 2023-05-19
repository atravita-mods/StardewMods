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
    public List<string>? GetPathFor(GameLocation start, GameLocation end, int gender, bool allowPartialPaths);
}
