namespace GiantCropFertilizer.HarmonyPatches;

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
    [HarmonyPatch(MethodType.Constructor, typeof(Vector2), typeof(int))]
    private static void Postfix(BigSlime __instance, int mineArea)
    {
        if (__instance.heldItem?.Value is not null)
        {
            return;
        }
        try
        {
            if (__instance.heldItem is not null
                && mineArea >= 120
                && Game1.mine?.GetAdditionalDifficulty() is > 0
                && Random.Shared.OfChance(0.05))
            {
                __instance.heldItem.Value = new SObject(ModEntry.GiantCropFertilizerID, 1);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding fertilizer to big slime", ex);
        }
    }
}