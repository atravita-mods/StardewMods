using AtraBase.Toolkit.Extensions;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Monsters;

namespace MoreFertilizers.HarmonyPatches.Acquisition;

/// <summary>
/// Holds patches against BigSlime's Vector2, int constructor.
/// </summary>
[HarmonyPatch(typeof(BigSlime))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class PostfixBigSlimeConstructor
{
    [UsedImplicitly]
    [HarmonyPatch(MethodType.Constructor, typeof(Vector2), typeof(int))]
    private static void Postfix(BigSlime __instance, int mineArea)
    {
        if (__instance.heldItem?.Value is not null || __instance.heldItem is null)
        {
            return;
        }
        try
        {
            if (mineArea >= 120
                && Game1.mine?.GetAdditionalDifficulty() is > 0
                && Random.Shared.OfChance(0.13))
            {
                if (ModEntry.DeluxeFruitTreeFertilizerID != -1 && Random.Shared.OfChance(0.33))
                {
                    __instance.heldItem.Value = new SObject(ModEntry.DeluxeFruitTreeFertilizerID, 1);
                }
                else if (ModEntry.DeluxeFishFoodID != -1 && Random.Shared.OfChance(0.5))
                {
                    __instance.heldItem.Value = new SObject(ModEntry.DeluxeFishFoodID, 1);
                }
                else if (ModEntry.SecretJojaFertilizerID != -1 && (Utility.hasFinishedJojaRoute() || Random.Shared.OfChance(0.2)))
                {
                    __instance.heldItem.Value = new SObject(ModEntry.SecretJojaFertilizerID, 1);
                }
                return;
            }
            if (ModEntry.LuckyFertilizerID != -1
                && mineArea >= 120
                && Game1.mine?.GetAdditionalDifficulty() is <= 0
                && Random.Shared.OfChance(0.04))
            {
                __instance.heldItem.Value = new SObject(ModEntry.LuckyFertilizerID, 1);
                return;
            }
            if (ModEntry.WisdomFertilizerID != -1
                && mineArea <= 120
                && Random.Shared.OfChance(0.15))
            { // big slimes are exceptionally rare in the normal mines.
                __instance.heldItem.Value = new SObject(ModEntry.WisdomFertilizerID, 1);
                return;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding fertilizers to big slimes", ex);
        }
    }
}