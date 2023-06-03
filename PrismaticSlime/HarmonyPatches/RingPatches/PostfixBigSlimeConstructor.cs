using AtraCore;

using AtraShared.Utils.Extensions;

using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using StardewValley.Objects;

namespace PrismaticSlime.HarmonyPatches.RingPatches;

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
        if (__instance.heldObject?.Value is not null || __instance.heldObject is null)
        {
            return;
        }
        try
        {
            if (ModEntry.PrismaticSlimeRing != -1
                && Singletons.Random.NextDouble() < 0.008 + Math.Min(Game1.player.team.AverageDailyLuck() / 10.0, 0.01) + Math.Min(Game1.player.LuckLevel / 400.0, 0.01))
            {
                __instance.heldObject.Value = new SObject(ModEntry.PrismaticSlimeRing, 1);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding ring to big slimes", ex);
        }
    }

    // swap out the SObject out for a proper ring.
    [UsedImplicitly]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(BigSlime.getExtraDropItems))]
    private static void GetExtraDropItemsPostfix(List<Item> __result)
    {
        if (ModEntry.PrismaticSlimeRing == -1)
        {
            return;
        }

        for (int i = 0; i < __result.Count; i++)
        {
            if (__result[i].ParentSheetIndex == ModEntry.PrismaticSlimeRing)
            {
                if (__result[i] is not SObject oldring || oldring.bigCraftable.Value || oldring.GetType() != typeof(SObject))
                {
                    continue;
                }

                ModEntry.ModMonitor.DebugOnlyLog("Fixing ring to be prismatic ring");
                __result[i] = new Ring(ModEntry.PrismaticSlimeRing);
                __result[i].modData.CopyModDataFrom(oldring.modData);
            }
        }
    }
}