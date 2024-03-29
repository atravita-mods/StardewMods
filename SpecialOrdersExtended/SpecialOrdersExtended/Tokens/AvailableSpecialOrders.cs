﻿namespace SpecialOrdersExtended.Tokens;

/// <summary>
/// Token that lists all available special orders.
/// </summary>
internal class AvailableSpecialOrders : AbstractToken
{
    /// <inheritdoc/>
    public override bool UpdateContext()
    {
        if (SpecialOrder.IsSpecialOrdersBoardUnlocked())
        {
            List<string>? specialOrderNames = Game1.player?.team?.availableSpecialOrders?.Where((s) => s?.questKey is not null)
                .Select((SpecialOrder s) => s.questKey.Value).OrderBy(a => a).ToList()
                ?? SaveGame.loaded?.availableSpecialOrders?.Where((s) => s?.questKey is not null)?.Select((SpecialOrder s) => s.questKey.Value)
                .OrderBy(a => a).ToList();
            return this.UpdateCache(specialOrderNames);
        }
        else
        {
            return this.UpdateCache(new List<string>());
        }
    }
}
