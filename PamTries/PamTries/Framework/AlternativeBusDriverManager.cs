using AtraBase.Toolkit.Extensions;
using AtraShared.Utils.Extensions;
using StardewModdingAPI.Events;

namespace PamTries.Framework;
internal static class AlternativeBusDriverManager
{
    private static HashSet<string> busdrivers = new(StringComparer.OrdinalIgnoreCase);

    internal static void MonitorSchedule(AssetReadyEventArgs e)
    {
        if (e.Name.IsDirectlyUnderPath("Characters/schedules"))
        {
            string name = e.Name.BaseName.GetNthChunk('/', 2).ToString();
            if (Game1.year < 2 && name.Equals("Kent", StringComparison.Ordinal))
            {
                return;
            }
            if (Game1.content.Load<Dictionary<string, string>>(e.Name.ToString()).ContainsKey("bus"))
            {
                if (busdrivers.Add(name))
                {
                    ModEntry.ModMonitor.Log($"Adding {name} to possible bus drivers.", LogLevel.Debug);
                }
            }
            else
            {
                if (busdrivers.Remove(name))
                {
                    ModEntry.ModMonitor.Log($"Remvoing {name} from possible bus drivers.", LogLevel.Debug);
                }
            }
        }
    }
}
