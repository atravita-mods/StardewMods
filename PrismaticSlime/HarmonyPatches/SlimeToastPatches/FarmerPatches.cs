using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

using PrismaticSlime.Framework;

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
    internal const string BuffId = "atravita.PrismaticJellyToast";

    [HarmonyPatch(nameof(Farmer.doneEating))]
    private static void Prefix(Farmer __instance)
    {
        if (__instance.itemToEat.QualifiedItemId != $"{ItemRegistry.type_object}{ModEntry.PrismaticJellyToast}")
        {
            return;
        }

        try
        {
            BuffEnum buffenum = BuffEnumExtensions.GetRandomBuff();
            Buff buff = buffenum.GetBuffOf(5, 2600, "Prismatic Toast", I18n.PrismaticJellyToast_Name(), id: BuffId);
            buff.iconTexture = AssetManager.BuffTexture;
            buff.iconSheetIndex = 0;
            buff.description = I18n.PrismaticJellyBuff_Description(buffenum.ToStringFast());
            buff.glow = Color.HotPink;

            Game1.player.applyBuff(buff);
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding prismatic toast buff", ex);
        }
    }
}
