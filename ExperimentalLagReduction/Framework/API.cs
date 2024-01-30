// Ignore Spelling: API

using AtraShared.ConstantsAndEnums;

using CommunityToolkit.Diagnostics;

using ExperimentalLagReduction.HarmonyPatches;

namespace ExperimentalLagReduction.Framework;

/// <inheritdoc />
public sealed class API : IExperimentalLagReductionAPI
{
    /// <inheritdoc />
    public bool ClearPathNulls() => Rescheduler.ClearNulls();

    /// <inheritdoc />
    public string[]? GetPathFor(GameLocation start, GameLocation end, Gender gender, bool allowPartialPaths)
    {
        Guard.IsNotNull(start);
        Guard.IsNotNull(end);

        if (Rescheduler.TryGetPathFromCache(start.Name, end.Name, gender, out string[]? path))
        {
            return path;
        }
        return Rescheduler.GetPathFor(start, end, (PathfindingGender)gender, allowPartialPaths);
    }

    /// <inheritdoc />
    public bool ClearMacroCache() => Rescheduler.ClearCache();

    /// <inheritdoc />
    public bool PrePopulateCache(bool parallel) => Rescheduler.PrePopulateCache(parallel);

    /// <inheritdoc />
    public void ResetGiftCache() => OverrideGiftTastes.Reset();
}
