using AtraShared.Utils.Extensions;
using HarmonyLib;

using StardewValley.Quests;

using AtraUtils = AtraShared.Utils.Utils;

namespace HolidaySales.HarmonyPatches;

/// <summary>
/// Adjusts quests of the day.
/// </summary>
[HarmonyPatch(typeof(Game1))]
internal static class AdjustQuestOfTheDay
{
    /// <summary>
    /// Prefixes RefreshQuestOfTheDay() to allow quests when there are events going on that are outside of town.
    /// </summary>
    /// <returns>True to continue to the vanilla function, false otherwise.</returns>
    [UsedImplicitly]
    [HarmonyPatch(nameof(Game1.RefreshQuestOfTheDay))]
    private static bool Prefix()
    {
        if (ModEntry.Config.StoreFestivalBehavior != FestivalsShopBehavior.Closed)
        {
            try
            {
                Quest? quest = null;

                // just get the quest if the shops are forced open.
                if (ModEntry.Config.StoreFestivalBehavior == FestivalsShopBehavior.Open)
                {
                    quest = Utility.getQuestOfTheDay();
                }

                // else, check if today or tomorrow is a festival day for vanilla locations.
                else if (!HSUtils.IsFestivalDayForMap(Game1.dayOfMonth, Game1.season, "Town"))
                {
                    (Season season, int day) = AtraUtils.GetTomorrow(Game1.season, Game1.dayOfMonth);
                    if (!HSUtils.IsFestivalDayForMap(day, season, "Town"))
                    {
                        quest = Utility.getQuestOfTheDay();
                    }
                }

                if (quest is not null)
                {
                    quest.reloadObjective();
                    quest.reloadDescription();
                }

                Game1.netWorldState.Value.SetQuestOfTheDay(quest);
                return false;
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError("adjusting Daily Quest", ex);
            }
        }
        return true;
    }
}
