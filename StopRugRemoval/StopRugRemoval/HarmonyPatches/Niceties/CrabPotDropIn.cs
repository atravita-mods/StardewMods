using HarmonyLib;

using StardewValley.Objects;
using StardewValley.Tools;

namespace StopRugRemoval.HarmonyPatches.Niceties;

/// <summary>
/// Holds patches against crab pots so you can bait them without taking the bait out of your rod.
/// </summary>
[HarmonyPatch(typeof(CrabPot))]
internal static class CrabPotDropIn
{
    [HarmonyPatch(nameof(CrabPot.performObjectDropInAction))]
    private static void Postfix(CrabPot __instance, Farmer who, ref bool __result, bool probe)
    {
        if (__instance.bait.Value is not null || __result || probe)
        {
            return;
        }

        if (who?.CurrentTool is not FishingRod rod || rod.attachments.Count < 1 || rod.attachments[0] is not SObject bait)
        {
            return;
        }

        if (Game1.getFarmer(__instance.owner.Value)?.professions.Contains(Farmer.mariner) ?? false)
        {
            return;
        }

        __instance.bait.Value = bait.getOne() as SObject;

        bait.Stack--;
        if (bait.Stack <= 0)
        {
            rod.attachments[0] = null;
        }

        __instance.owner.Value = who.UniqueMultiplayerID;
        __instance.Location.playSound("Ship");
        __instance.lidFlapping = true;
        __instance.lidFlapTimer = 60;
    }
}
