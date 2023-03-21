using AtraShared.Utils.Extensions;

using StardewModdingAPI.Events;

namespace GingerIslandMainlandAdjustments.DialogueChanges;

internal static class MarriageDialogueHandler
{
    [EventPriority(EventPriority.Low - 2000)]
    internal static void OnDayStart(object? sender, DayStartedEventArgs e)
    {
        try
        {
            if (Game1.player?.getSpouse() is NPC spouse && Game1.IsVisitingIslandToday(spouse.Name))
            {
                if (spouse.TryApplyMarriageDialogueIfExisting("GILeave_" + spouse.Name))
                {
                    Globals.ModMonitor.DebugOnlyLog($"Setting GILeave_{spouse?.Name}", LogLevel.Trace);
                }
                else if (Game1.player is not null)
                {
                    spouse.CurrentDialogue.Clear();
                    spouse.currentMarriageDialogue.Clear();
                    if (Game1.player.getFriendshipHeartLevelForNPC(spouse.Name) > 9)
                    {
                        spouse.CurrentDialogue.Push(new Dialogue(I18n.GILeaveDefaultHappy(spouse.getTermOfSpousalEndearment()), spouse));
                    }
                    else
                    {
                        spouse.CurrentDialogue.Push(new Dialogue(I18n.GILeaveDefaultUnhappy(), spouse));
                    }
                    Globals.ModMonitor.DebugOnlyLog($"Setting default GILeave dialogue.", LogLevel.Trace);
                }
            }
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.Log($"Error in setting GILeave dialogue:\n{ex}", LogLevel.Error);
        }
    }
}