namespace SpecialOrdersExtended.Tokens;

internal class CompletedSpecialOrders : AbstractToken
{
    ///<inheritdoc/>
    public override bool UpdateContext()
    {
        List<string>? specialOrderNames;
        if (Context.IsWorldReady)
        {
            specialOrderNames = Game1.player.team.completedSpecialOrders.Keys.OrderBy(a => a)?.ToList();
        }
        else
        {
            specialOrderNames = SaveGame.loaded?.completedSpecialOrders?.OrderBy(a => a)?.ToList();
        }
        return this.UpdateCache(specialOrderNames);
    }
}
