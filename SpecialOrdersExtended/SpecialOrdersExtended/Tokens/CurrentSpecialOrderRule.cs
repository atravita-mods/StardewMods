namespace SpecialOrdersExtended.Tokens;

internal class CurrentSpecialOrderRule : AbstractToken
{
    public override bool UpdateContext()
    {
        List<string>? rules;
        if (Context.IsWorldReady)
        {
            rules = Game1.player.team.specialOrders
                .SelectMany((SpecialOrder i) => i.specialRule.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .OrderBy(a => a).ToList();
        }
        else
        {
            rules = SaveGame.loaded?.specialOrders
                ?.SelectMany((SpecialOrder i) => i.specialRule.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                ?.OrderBy(a => a)?.ToList();
        }

        if (rules == SpecialOrdersCache)
        {
            return false;
        }
        else
        {
            SpecialOrdersCache = rules;
            return true;
        }
    }
}
