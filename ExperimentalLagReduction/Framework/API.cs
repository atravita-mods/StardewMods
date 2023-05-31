// Ignore Spelling: API

using AtraShared.ConstantsAndEnums;

using CommunityToolkit.Diagnostics;

using ExperimentalLagReduction.HarmonyPatches;

namespace ExperimentalLagReduction.Framework;

/// <inheritdoc />
public sealed class API : IExperimentalLagReductionAPI
{
    /// <inheritdoc />
    public List<string>? GetPathFor(GameLocation start, GameLocation end, int gender, bool allowPartialPaths)
    {
        Guard.IsNotNull(start);
        Guard.IsNotNull(end);

        if (Rescheduler.TryGetPathFromCache(start.Name, end.Name, gender, out List<string>? path))
        {
            return path;
        }
        return Rescheduler.GetPathFor(start, end, (Gender)gender, allowPartialPaths);
    }

    /// <inheritdoc />
    public void ResetGiftCache() => OverrideGiftTastes.Reset();
}
