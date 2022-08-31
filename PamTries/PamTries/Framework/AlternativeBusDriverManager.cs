using AtraBase.Toolkit.Extensions;
using AtraShared.Utils.Extensions;
using StardewModdingAPI.Events;

namespace PamTries.Framework;

/// <summary>
/// Manages alternative bus drivers.
/// </summary>
internal static class AlternativeBusDriverManager
{
    private static HashSet<string> busdrivers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a random bus driver from the alternative bus drivers list.
    /// </summary>
    /// <returns>Name of a possible bus driver.</returns>
    internal static string GetRandomBusDriver()
        => busdrivers.Count == 0 ? "Pam" : busdrivers.ElementAt(Game1.random.Next(busdrivers.Count));

    /// <summary>
    /// Listens to AssetReady to find valid bus drivers.
    /// </summary>
    /// <param name="e">event args.</param>
    internal static void MonitorSchedule(AssetReadyEventArgs e)
    {
        if (e.Name.IsDirectlyUnderPath("Characters/schedules"))
        {
            string name = e.Name.BaseName.GetNthChunk('/', 2).ToString();
            if ((Game1.year < 2 && name.Equals("Kent", StringComparison.OrdinalIgnoreCase))
                || name.Equals("Pam", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (Game1.content.Load<Dictionary<string, string>>(e.Name.ToString()).ContainsKey("bus"))
            {
                if (busdrivers.Add(name))
                {
                    ModEntry.ModMonitor.DebugOnlyLog($"Adding {name} to possible bus drivers.", LogLevel.Debug);
                }
            }
            else
            {
                if (busdrivers.Remove(name))
                {
                    ModEntry.ModMonitor.DebugOnlyLog($"Removing {name} from possible bus drivers.", LogLevel.Debug);
                }
            }
        }
    }
}
