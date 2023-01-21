using AtraShared.ConstantsAndEnums;

using HarmonyLib;

namespace PrismaticSlime.HarmonyPatches.SlimeToastPatches;

[HarmonyPatch(typeof(Farmer))]
internal static class FarmerPatches
{
    private const int ID = 45674642; // TODO - use the actual id.

    [HarmonyPatch(nameof(Farmer.doneEating))]
    private static void Prefix(Farmer __instance)
    {
        if (!Utility.IsNormalObjectAtParentSheetIndex(__instance.itemToEat, ModEntry.PrismaticJellyToast))
        {
            return;
        }

        try
        {
            BuffEnum buffenum = BuffEnumExtensions.GetRandomBuff();
            Buff buff = buffenum.GetBuffOf(5, 2600, "Prismatic Toast", I18n.PrismaticJellyToast_Name());
            buff.which = ID;
            buff.sheetIndex = 0;
            buff.description = I18n.PrismaticJellyBuff_Description(buffenum.ToStringFast());

            Game1.buffsDisplay.addOtherBuff(buff);
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed while trying to add prismatic toast buff\n\n{ex}", LogLevel.Error);
        }
    }
}
