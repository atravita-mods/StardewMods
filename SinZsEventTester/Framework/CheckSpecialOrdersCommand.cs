using StardewValley.GameData.SpecialOrders;
using StardewValley.SpecialOrders;

using static StardewValley.Menus.CoopMenu;

namespace SinZsEventTester.Framework;

/// <summary>
/// A class that helps check for issues in special orders.
/// </summary>
/// <param name="monitor">The monitor instance.</param>
internal sealed class CheckSpecialOrdersCommand(IMonitor monitor)
{

    /// <summary>
    /// checks for the available special orders.
    /// </summary>
    internal void GetAvailableOrders()
    {
        if (!Context.IsWorldReady)
        {
            monitor.Log(I18n.LoadSaveFirst(), LogLevel.Warn);
        }
        Dictionary<string, SpecialOrderData> order_data = DataLoader.SpecialOrders(Game1.content);
        IOrderedEnumerable<string> keys = order_data.Keys.OrderBy(a => a);

        List<string> validkeys = [];
        List<string> unseenkeys = [];

        var count = 0;
        foreach (string key in keys)
        {
            count++;
            SpecialOrderData order = order_data[key];
            if (this.IsAvailableOrder(key, order))
            {
                monitor.VerboseLog($"\t{key} is valid");
                validkeys.Add(key);
                if (!Game1.MasterPlayer.team.completedSpecialOrders.Contains(key))
                {
                    unseenkeys.Add(key);
                }
            }
        }
        monitor.Log($"Total keys found: {count}", LogLevel.Debug);
        monitor.Log($"{I18n.ValidKeys(count: validkeys.Count)}: {string.Join(", ", validkeys)}", LogLevel.Debug);
        monitor.Log($"{I18n.UnseenKeys(count: unseenkeys.Count)}: {string.Join(", ", unseenkeys)}", LogLevel.Debug);
    }

    private bool IsAvailableOrder(string key, SpecialOrderData order)
    {
        monitor.Log($"{I18n.Analyzing()} {key}", LogLevel.Debug);
        try
        {
            SpecialOrder? specialOrder = SpecialOrder.GetSpecialOrder(key, Random.Shared.Next());
            if (specialOrder is not null)
            {
                monitor.Log($"\t{key} {I18n.Parsable()}", LogLevel.Debug);
                if (specialOrder.orderType.Value.Length != 0 && specialOrder.orderType.Value != "Qi")
                {
                    monitor.Log($"\t\tNon-vanilla special order type {specialOrder.orderType.Value}", LogLevel.Debug);
                }
            }
            else
            {
                monitor.Log($"\t{key} {I18n.Unparsable()}", LogLevel.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            monitor.Log($"\t{key} {I18n.Unparsable()}\n{ex}", LogLevel.Error);
            return false;
        }

        bool seen = Game1.MasterPlayer.team.completedSpecialOrders.Contains(key);
        if (!order.Repeatable && seen)
        {
            monitor.Log($"\t{I18n.Nonrepeatable()}", LogLevel.Debug);
            return false;
        }
        else if (seen)
        {
            monitor.Log($"\t{I18n.RepeatableSeen()}", LogLevel.Debug);
        }
        if (Game1.dayOfMonth >= 16 && order.Duration == QuestDuration.Month)
        {
            monitor.Log($"\t{I18n.MonthLongLate(cutoff: 16)}");
            return false;
        }
        if (!GameStateQuery.CheckConditions(order.Condition))
        {
            monitor.Log($"\t{I18n.FailedGSQ()}: {order.Condition}", LogLevel.Debug);
        }
        if (!SpecialOrder.CheckTags(order.RequiredTags))
        {
            monitor.Log($"\t{I18n.HasInvalidTags()}:", LogLevel.Debug);
            foreach (string tag in order.RequiredTags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                bool match = true;
                if (tag.Length == 0)
                {
                    continue;
                }
                string trimmed_tag;
                if (tag.StartsWith('!'))
                {
                    match = false;
                    trimmed_tag = tag[1..];
                }
                else
                {
                    trimmed_tag = tag;
                }

                if (SpecialOrder.CheckTag(trimmed_tag) != match)
                {
                    monitor.Log($"\t\t{I18n.TagFailed()}: {tag}", LogLevel.Debug);
                }
            }
            return false;
        }
        if (!SpecialOrder.CanStartOrderNow(key, order))
        {
            monitor.Log($"\t{I18n.Unknown()}", LogLevel.Debug);
        }
        foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
        {
            if (specialOrder.questKey.Value == key)
            {
                monitor.Log($"\t{I18n.Active()}", LogLevel.Debug);
                return false;
            }
        }
        return true;
    }
}
