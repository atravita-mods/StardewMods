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
            if (Game1.content.Load<Dictionary<string, string>>(e.Name.ToString()).ContainsKey("bus"))
            {
                string name = e.Name.BaseName.GetNthChunk('/', 2).ToString();
                busdrivers.Add(name);

                ModEntry.ModMonitor.Log($"Adding {name} to possible bus drivers.", LogLevel.Debug);
            }
        }
    }
}
