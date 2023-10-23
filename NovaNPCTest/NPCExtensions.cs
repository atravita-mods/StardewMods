using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaNPCTest;
internal static class NPCExtensions
{
    /// <summary>
    /// Helper method to get an NPC's raw schedule string for a specific key.
    /// </summary>
    /// <param name="npc">NPC in question.</param>
    /// <param name="scheduleKey">Schedule key to look for.</param>
    /// <param name="rawData">Raw schedule string.</param>
    /// <returns>True if successful, false otherwise.</returns>
    /// <remarks>Does **not** set _lastLoadedScheduleKey, intentionally.</remarks>
    public static bool TryGetScheduleEntry(
        this NPC npc,
        string scheduleKey,
        [NotNullWhen(returnValue: true)] out string? rawData)
    {
        rawData = null;
        Dictionary<string, string>? scheduleData = npc.getMasterScheduleRawData();
        if (scheduleData is null || scheduleKey is null)
        {
            return false;
        }
        return scheduleData.TryGetValue(scheduleKey, out rawData);
    }
}
