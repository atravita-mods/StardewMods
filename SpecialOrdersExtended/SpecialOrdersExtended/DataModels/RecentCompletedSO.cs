using System;
using System.Collections.Generic;
using System.Text;
using StardewModdingAPI;

using StardewValley;

namespace SpecialOrdersExtended.DataModels
{
    internal class RecentCompletedSO : AbstractDataModel
    {
        private const string identifier = "_SOmemory";

        public Dictionary<string, int> RecentOrdersCompleted { get; set; } = new();

        public RecentCompletedSO(string savefile)
        {
            this.Savefile = savefile;
        }

        public static RecentCompletedSO Load()
        {
            if (!Context.IsWorldReady) { throw new SaveNotLoadedError(); }
            return ModEntry.DataHelper.ReadGlobalData<RecentCompletedSO>(Constants.SaveFolderName + identifier) ?? new RecentCompletedSO(Constants.SaveFolderName);
        }

        public void Save()
        {
            base.Save(identifier);
        }

        public void dayUpdate(int daysPlayed)
        {
            foreach (string key in RecentOrdersCompleted.Keys)
            {
                if (daysPlayed > RecentOrdersCompleted[key] + 7)
                {
                    RecentOrdersCompleted.Remove(key);
                }
            }
        }

        public void Add(string orderKey, int daysPlayed)
        {
            RecentOrdersCompleted[orderKey] = daysPlayed;
        }

        public bool Remove(string orderKey)
        {
            return RecentOrdersCompleted.Remove(orderKey);
        }

        public bool IsWithinXDays(string orderKey, int days)
        {
            if (RecentOrdersCompleted.TryGetValue(orderKey, out int dayCompleted))
            {
                return dayCompleted + days > Game1.stats.daysPlayed;
            }
            return false;
        }

        //ToString?
    }
}
