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

        public Dictionary<string, uint> RecentOrdersCompleted { get; set; } = new();

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

        public void dayUpdate(uint daysPlayed)
        {
            foreach (string key in RecentOrdersCompleted.Keys)
            {
                if (daysPlayed > RecentOrdersCompleted[key] + 7)
                {
                    RecentOrdersCompleted.Remove(key);
                }
            }
        }

        public bool Add(string orderKey, uint daysPlayed)
        {
            return RecentOrdersCompleted.TryAdd(orderKey, daysPlayed);
        }

        public bool Remove(string orderKey)
        {
            return RecentOrdersCompleted.Remove(orderKey);
        }

        public bool IsWithinXDays(string orderKey, uint days)
        {
            if (RecentOrdersCompleted.TryGetValue(orderKey, out uint dayCompleted))
            {
                return dayCompleted + days > Game1.stats.daysPlayed;
            }
            return false;
        }

        //ToString?
    }
}
