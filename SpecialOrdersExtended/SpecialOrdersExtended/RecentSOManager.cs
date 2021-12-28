using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using StardewModdingAPI;

using StardewValley;

using SpecialOrdersExtended.DataModels;

namespace SpecialOrdersExtended
{
    internal class RecentSOManager
    {
        private static RecentCompletedSO recentCompletedSO;

        public static void Load() => recentCompletedSO = RecentCompletedSO.Load();

        public static void Save() => recentCompletedSO.Save();

        public static bool Add(string questkey)
        {
            if (!Context.IsWorldReady) { throw new SaveNotLoadedError(); }
            return recentCompletedSO.Add(questkey, Game1.stats.DaysPlayed);
        }

        public static bool Remove(string questkey)
        {
            if (!Context.IsWorldReady) { throw new SaveNotLoadedError(); }
            return recentCompletedSO.Remove(questkey);
        }

        public static bool IsWithinXDays(string questkey, uint days )
        {
            if (!Context.IsWorldReady) { throw new SaveNotLoadedError(); }
            return recentCompletedSO.IsWithinXDays(questkey, days);
        }
    }
}
