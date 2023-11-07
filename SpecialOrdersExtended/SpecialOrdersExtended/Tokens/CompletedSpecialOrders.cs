namespace SpecialOrdersExtended.Tokens;

/// <summary>
/// Token that gets all completed special orders.
/// </summary>
internal class CompletedSpecialOrders : AbstractToken
{
    /// <inheritdoc/>
    public override bool UpdateContext()
    {
        List<string>? specialOrderNames;
        if (Context.IsWorldReady)
        {
            specialOrderNames = Game1.player.team.completedSpecialOrders.OrderBy(a => a)?.ToList();
        }
        else
        {
            specialOrderNames = SaveGame.loaded?.completedSpecialOrders?.OrderBy(a => a)?.ToList();
        }
        return this.UpdateCache(specialOrderNames);
    }
}
