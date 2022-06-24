using System;
using System.Collections.Generic;
using System.Text;
using StardewModdingAPI.Utilities;

namespace AtraShared.Utils;

internal static class DateHelper
{
    internal static bool IsWinterStarLetterValid(WorldDate date)
        => date.SeasonIndex == 3 && date.DayOfMonth >= 18 && date.DayOfMonth <= 25;

    internal static bool IsWinterStarLetterValid(SDate date)
        => date.SeasonIndex == 3 && date.Day >= 18 && date.Day <= 25;

#warning - fix in Stardew 1.6
    internal static bool IsWinterStarLetterValidToday()
        => Game1.currentSeason is "winter" && Game1.dayOfMonth >= 18 && Game1.dayOfMonth <= 25;
}
