using GingerIslandMainlandAdjustments.ScheduleManager;

using HarmonyLib;

using StardewValley.Locations;

namespace GingerIslandMainlandAdjustments.DialogueChanges;

[HarmonyPatch(typeof(IslandSouth))]
internal static class PatchesOnIslandSouth
{
    [HarmonyPatch(nameof(IslandSouth.GetLocationOverrideDialogue))]
    private static void Postfix(NPC character, ref string __result)
    {
        if (GIScheduler.CurrentVisitingGroup?.Contains(character) != true || GIScheduler.CurrentGroup is not string group)
        {
            return;
        }

        if (Game1.timeOfDay < 1200 || (!character.shouldWearIslandAttire.Value && Game1.timeOfDay < 1730 && IslandSouth.HasIslandAttire(character)))
        {
            if (character.Dialogue.ContainsKey("Resort_Entering_" + group))
            {
                __result = @$"Characters\Dialogue\{character.Name}:Resort_Entering_{group}";
                return;
            }
        }
        if (Game1.timeOfDay >= 1800)
        {
            if (character.Dialogue.ContainsKey("Resort_Leaving_" + group))
            {
                __result = $@"Characters\Dialogue\{character.Name}:Resort_Leaving_{group}";
                return;
            }
        }

        if (character.Dialogue.ContainsKey("Resort_" + group))
        {
            __result = $@"Characters\Dialogue\{character.Name}:Resort_{group}";
        }
    }
}
