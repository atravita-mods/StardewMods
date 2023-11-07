using StardewValley.SpecialOrders;

namespace SpecialOrdersExtended.Tokens;

/// <summary>
/// Token that gets all current active special order rules.
/// </summary>
internal class CurrentSpecialOrderRule : AbstractToken
{
    /// <inheritdoc/>
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

        return this.UpdateCache(rules);
    }
}
