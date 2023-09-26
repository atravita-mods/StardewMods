using StardewValley.SpecialOrders;

namespace SpecialOrdersExtended.Tokens;

/// <summary>
/// Token that gets all current special orders.
/// </summary>
internal class CurrentSpecialOrders : AbstractToken
{
    /// <inheritdoc/>
    public override bool UpdateContext()
    {
        List<string>? specialOrderNames = Game1.player?.team?.specialOrders?.Select((SpecialOrder s) => s.questKey.Value)?.OrderBy(a => a)?.ToList()
            ?? SaveGame.loaded?.specialOrders?.Select((SpecialOrder s) => s.questKey.Value)?.OrderBy(a => a)?.ToList();
        return this.UpdateCache(specialOrderNames);
    }
}
