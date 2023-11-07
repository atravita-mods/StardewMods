﻿using AtraUtils = AtraShared.Utils.Utils;

namespace GingerIslandMainlandAdjustments.Utils;

/// <summary>
/// Methods to handle the current set of Islanders.
/// </summary>
internal static class Islanders
{
    /// <summary>
    /// Gets a list of Islanders.
    /// </summary>
    /// <returns>Sorted list of Islanders by name.</returns>
    internal static List<string> Get()
    {
        List<string> islanders = Game1.netWorldState.Value.IslandVisitors.ToList();
        return AtraUtils.ContextSort(islanders);
    }
}
