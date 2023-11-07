using AtraShared.Utils.Extensions;

using StardewModdingAPI.Events;

namespace GingerIslandMainlandAdjustments.DialogueChanges;

/// <summary>
/// Handles adding marriage dialogue.
/// </summary>
internal static class MarriageDialogueHandler
{
    /// <summary>
    /// Handles adding marriage dialogue on day start.
    /// Using a low event priority to slot after Custom NPC Exclusions.
    /// </summary>
    [EventPriority(EventPriority.Low - 2000)]
    internal static void OnDayStart()
    {
        try
        {
            if (Game1.player?.getSpouse() is NPC spouse && Game1.IsVisitingIslandToday(spouse.Name))
            {
                if (spouse.TryApplyMarriageDialogueIfExisting("GILeave_" + spouse.Name))
                {
                    Globals.ModMonitor.DebugOnlyLog($"Setting GILeave_{spouse.Name}", LogLevel.Trace);
                }
                else if (Game1.player is not null)
                {
                    spouse.CurrentDialogue.Clear();
                    spouse.currentMarriageDialogue.Clear();
                    if (Game1.player.getFriendshipHeartLevelForNPC(spouse.Name) > 9)
                    {
                        spouse.CurrentDialogue.Push(new Dialogue(spouse, null, I18n.GILeaveDefaultHappy(spouse.getTermOfSpousalEndearment())));
                    }
                    else
                    {
                        spouse.CurrentDialogue.Push(new Dialogue(spouse, null, I18n.GILeaveDefaultUnhappy()));
                    }
                    Globals.ModMonitor.DebugOnlyLog($"Setting default GILeave dialogue.", LogLevel.Trace);
                }
            }
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.LogError("setting GILeave dialogue", ex);
        }
    }
}