using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;

using StardewValley;
using System.Reflection;

namespace SpecialOrdersExtended
{
    internal class StatsManager
    {
        Dictionary<string, PropertyInfo> propertyInfos = new();

        //remove these stats, they make no sense.
        private readonly string[] denylist = { "AverageBedtime", "TimesUnconscious", "TotalMoneyGifted" };


        /// <summary>
        /// Populate the propertyInfos cache.
        /// </summary>
        public void GrabProperties()
        {
            propertyInfos = typeof(Stats).GetProperties()
                .Where( (PropertyInfo p) => p.CanRead && p.PropertyType.Equals(typeof(uint)) && !denylist.Contains(p.Name))
                .ToDictionary((PropertyInfo p)=> p.Name.ToLowerInvariant(), p => p);
        }

        public void ClearProperties()
        {
            propertyInfos.Clear();
        }

        public uint GrabBasicProperty(string key, Stats stats)
        {
            if (propertyInfos.Count.Equals(0)) { GrabProperties(); }
            try
            {
                if (propertyInfos.TryGetValue(key.ToLowerInvariant(), out PropertyInfo property))
                {
                    return (uint)property.GetValue(stats);
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Failure to use {key}, please take this log to https://github.com/atravita-mods/SpecialOrdersExtended/issues \n\n{ex}", LogLevel.Error);
            }
            if (stats.stat_dictionary.TryGetValue(key, out uint result)){ return result;}
            ModEntry.ModMonitor.Log($"{key} is not found in stats.",LogLevel.Trace);
            return 0u;
        }

        public void ConsoleListProperties(string command, string[] args)
        {
            if (propertyInfos.Count.Equals(0)) { GrabProperties(); }
            ModEntry.ModMonitor.Log($"{ModEntry.I18n.Get("current-keys-found")}: \n    {ModEntry.I18n.Get("hardcoded")}:{String.Join(", ", propertyInfos.Keys)}\n    {ModEntry.I18n.Get("dictionary")}:{String.Join(", ", Game1.player.stats.stat_dictionary.Keys)}", LogLevel.Info);
        }
    }
}
