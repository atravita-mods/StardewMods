namespace SpecialOrdersExtended.Tokens;

/// <summary>
/// Token that lists all available special orders.
/// </summary>
internal class AvailableSpecialOrders : AbstractToken
{
    /// <inheritdoc/>
    public override bool UpdateContext()
    {
        List<string>? specialOrderNames = Game1.player?.team?.availableSpecialOrders?.Select((SpecialOrder s) => s.questKey.ToString()).OrderBy(a => a).ToList()
            ?? SaveGame.loaded?.availableSpecialOrders?.Select((SpecialOrder s) => s.questKey.ToString()).OrderBy(a => a).ToList();
        return this.UpdateCache(specialOrderNames);
    }
}
