namespace TrashDoesNotConsumeBait.HarmonyPatches;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using StardewValley.Objects;

/// <summary>
/// Patches on CrabPot to restore bait if the object was trash.
/// </summary>
[HarmonyPatch(typeof(CrabPot), nameof(CrabPot.checkForAction))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class CrabPotPatches
{
    [HarmonyPriority(Priority.First)]
    private static void Prefix(CrabPot __instance, bool justCheckingForActivity, out SObject? __state)
    {
        if (!justCheckingForActivity && ModEntry.Config.CrabPotTrashDoesNotEatBait && __instance.heldObject.Value?.IsTrashItem() == true)
        {
            __state = __instance.bait.Value;
        }
        else
        {
            __state = null;
        }
    }

    private static void Postfix(CrabPot __instance, bool justCheckingForActivity, SObject? __state)
    {
        if (!justCheckingForActivity && __state is not null && __instance.bait.Value is null)
        {
            __instance.bait.Value = __state;
        }
    }
}