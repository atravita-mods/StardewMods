namespace SpecialOrdersExtended.Tokens;

internal class RecentCompletedSO : AbstractToken
{
    ///<inheritdoc/>
    public override bool UpdateContext()
    {
        List<string>? recentCompletedSO = RecentSOManager.GetKeys(7u)?.OrderBy(a => a)?.ToList();
        return this.UpdateCache(recentCompletedSO);
    }
}
