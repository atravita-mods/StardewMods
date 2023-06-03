using AtraCore;

using AtraShared.Utils.Extensions;

using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Monsters;

namespace GiantCropFertilizer.HarmonyPatches;

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
        if (__instance.heldObject?.Value is not null || ModEntry.GiantCropFertilizerID == -1)
        {
            return;
        }
        try
        {
            if (__instance.heldObject is not null
                && mineArea >= 120
                && Game1.mine?.GetAdditionalDifficulty() is > 0
                && Singletons.Random.NextDouble() < 0.05)
            {
                __instance.heldObject.Value = new SObject(ModEntry.GiantCropFertilizerID, 1);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding fertilizer to big slime", ex);
        }
    }
}