namespace SpecialOrdersExtended.Tokens;

internal class CurrentSpecialOrders : AbstractToken
{
    ///<inheritdoc/>
    public override bool UpdateContext()
    {
        List<string>? specialOrderNames = Game1.player?.team?.specialOrders?.Select((SpecialOrder s) => s.questKey.ToString())?.OrderBy(a => a)?.ToList()
            ?? SaveGame.loaded?.specialOrders?.Select((SpecialOrder s) => s.questKey.ToString())?.OrderBy(a => a)?.ToList();
        return this.UpdateCache(specialOrderNames);
    }
}
