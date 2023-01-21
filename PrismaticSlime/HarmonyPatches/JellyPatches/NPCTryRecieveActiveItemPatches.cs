using AtraShared.ConstantsAndEnums;

using HarmonyLib;

namespace PrismaticSlime.HarmonyPatches.JellyPatches;

[HarmonyPatch(typeof(NPC))]
internal static class NPCTryRecieveActiveItemPatches
{
    [HarmonyPatch(nameof(NPC.tryToReceiveActiveObject))]
    private static bool Prefix(NPC __instance, Farmer who)
    {
        if (Utility.IsNormalObjectAtParentSheetIndex(who.ActiveObject, ModEntry.PrismaticJelly)
            || who.team.specialOrders.Any(order => order.questKey.Value == "Wizard2"))
        {
            try
            {
                // TODO
                if (__instance.Name == "Wizard")
                {
                    // Wizard stuff
                    BuffEnum buffEnum = BuffEnumExtensions.GetRandomBuff();
                }
                else
                {
                    // dialogue, friendship.
                }
                return false;
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Failed while trying to override NPC.{nameof(NPC.tryToReceiveActiveObject)}\n\n{ex}", LogLevel.Error);
            }
        }

        return true;
    }
}
