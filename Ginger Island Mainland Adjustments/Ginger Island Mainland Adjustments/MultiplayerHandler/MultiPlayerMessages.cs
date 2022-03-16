using AtraShared.Utils.Extensions;
using HarmonyLib;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace GingerIslandMainlandAdjustments.MultiplayerHandler;

[HarmonyPatch(typeof(NPC))]
public static class MultiplayerSharedState
{
    private const string SCHEDULEMESSAGE = "GIMAScheduleUpdateMessage";
    internal static string? PamsSchedule { get; set; }

    /// <summary>
    /// Updates entry for Pam's schedule whenever a person joins in multiplayer.
    /// </summary>
    /// <param name="e">arguments?</param>
    internal static void ReSendMultiplayerMessage(PeerConnectedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady && Game1.getCharacterFromName("Pam") is NPC pam 
            && pam.TryGetScheduleEntry(pam.dayScheduleName.Value, out string? rawstring)
            && Globals.UtilitySchedulingFunctions.TryFindGOTOschedule(pam, SDate.Now(), rawstring, out string redirectedstring))
        {
            PamsSchedule = redirectedstring;
            Globals.ModMonitor.Log($"Grabbing Pam's rawSchedule for phone: {redirectedstring}");
            Globals.Helper.Multiplayer.SendMessage(redirectedstring, SCHEDULEMESSAGE, modIDs: new[] { Globals.Manifest.UniqueID });
        }
    }

    internal static void UpdateFromMessage(ModMessageReceivedEventArgs e)
    {
        if (e.FromModID == Globals.Manifest.UniqueID && e.Type == SCHEDULEMESSAGE)
        {
            PamsSchedule = e.ReadAs<string>();
            Globals.ModMonitor.Log($"Recieved Pam's schedule {PamsSchedule}", LogLevel.Debug);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NPC.parseMasterSchedule))]
    private static void PostfixGetMasterSchedule(NPC __instance)
    {
        try
        {
            if (Context.IsMainPlayer && __instance is NPC && __instance.Name.Equals("Pam", StringComparison.OrdinalIgnoreCase)
                && __instance.TryGetScheduleEntry(__instance.dayScheduleName.Value, out string? rawstring)
                && Globals.UtilitySchedulingFunctions.TryFindGOTOschedule(__instance, SDate.Now(), rawstring, out string redirectedstring))
            {
                PamsSchedule = redirectedstring;
                Globals.ModMonitor.Log($"Grabbing Pam's rawSchedule for phone: {redirectedstring}");
                Globals.Helper.Multiplayer.SendMessage(redirectedstring, SCHEDULEMESSAGE, modIDs: new[] { Globals.Manifest.UniqueID });
            }
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.Log($"Error in postfixing get master schedule to get Pam's schedule.\n\n{ex}", LogLevel.Error);
        }
    }
}