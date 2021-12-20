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


    }
}
