namespace SpecialOrdersExtended.Tokens;

internal class RecentCompletedSO : AbstractToken
{
    public override bool UpdateContext()
    {
        List<string>? recentCompletedSO = RecentSOManager.GetKeys(7u)?.OrderBy(a => a)?.ToList();
        if (recentCompletedSO == this.SpecialOrdersCache)
        {
            return false;
        }
        else
        {
            this.SpecialOrdersCache = recentCompletedSO;
            return true;
        }
    }
}
