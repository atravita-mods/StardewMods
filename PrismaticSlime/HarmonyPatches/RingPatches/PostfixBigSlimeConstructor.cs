namespace PrismaticSlime.HarmonyPatches.RingPatches;

using AtraBase.Toolkit.Extensions;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Monsters;

/// <summary>
/// Holds patches against BigSlime's Vector2, int constructor.
/// </summary>
[HarmonyPatch(typeof(BigSlime))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class PostfixBigSlimeConstructor
{
    [UsedImplicitly]
    [HarmonyPostfix]
    [HarmonyPatch(MethodType.Constructor, typeof(Vector2), typeof(int))]
    private static void PostfixConstructor(BigSlime __instance)
    {
        if (__instance.heldItem?.Value is not null || __instance.heldItem is null)
        {
            return;
        }
        try
        {
            if (Random.Shared.OfChance(0.008 + Math.Min(Game1.player.team.AverageDailyLuck() / 10.0, 0.01) + Math.Min(Game1.player.LuckLevel / 400.0, 0.01)))
            {
                __instance.heldItem.Value = ItemRegistry.Create($"{ItemRegistry.type_object}{ModEntry.PrismaticSlimeRing}");
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding ring to big slimes", ex);
        }
    }
}