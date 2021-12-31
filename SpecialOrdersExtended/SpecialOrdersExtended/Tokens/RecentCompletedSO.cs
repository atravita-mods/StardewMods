namespace SpecialOrdersExtended.Tokens;

internal class RecentCompletedSO : AbstractToken
{
    public override bool UpdateContext()
    {
        List<string> recentCompletedSO = RecentSOManager.GetKeys(7u)?.OrderBy(a => a)?.ToList();
        if (recentCompletedSO == SpecialOrdersCache)
        {
            return false;
        }
        else
        {
            SpecialOrdersCache = recentCompletedSO;
            return true;
        }
    }
}
