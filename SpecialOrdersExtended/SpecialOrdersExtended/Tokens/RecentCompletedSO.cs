﻿using SpecialOrdersExtended.Managers;

namespace SpecialOrdersExtended.Tokens;

/// <summary>
/// Token that gets all Special Orders completed within the last seven in-game days.
/// </summary>
internal class RecentCompletedSO : AbstractToken
{
    /// <inheritdoc/>
    public override bool UpdateContext()
    {
        List<string>? recentCompletedSO = RecentSOManager.GetKeys(7u)?.OrderBy(a => a)?.ToList();
        return this.UpdateCache(recentCompletedSO);
    }
}
