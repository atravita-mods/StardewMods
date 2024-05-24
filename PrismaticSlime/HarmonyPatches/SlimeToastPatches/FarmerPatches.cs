using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

namespace PrismaticSlime.HarmonyPatches.SlimeToastPatches;

/// <summary>
/// Holds patches against Farmer.
/// </summary>
[HarmonyPatch(typeof(Farmer))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class FarmerPatches
{
    /// <summary>
    /// The ID number for the prismatic jelly toast buff.
    /// </summary>
    internal const int BuffId = 15157;

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
            buff.which = BuffId;
            buff.sheetIndex = 0;
            buff.description = I18n.PrismaticJellyBuff_Description(buffenum.ToStringFast());
            buff.glow = Color.HotPink;

            Game1.buffsDisplay.addOtherBuff(buff);
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding prismatic toast buff", ex);
        }
    }
}
