using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Monsters;

namespace GiantCropFertilizer.HarmonyPatches;

/// <summary>
/// Holds patches against BigSlime's Vector2, int constructor.
/// </summary>
[HarmonyPatch(typeof(BigSlime))]
internal static class PostfixBigSlimeConstructor
{
    [UsedImplicitly]
    [HarmonyPatch(MethodType.Constructor, typeof(Vector2), typeof(int))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention")]
    private static void Postfix(BigSlime __instance, int mineArea)
    {
        if (__instance.heldObject?.Value is not null || ModEntry.GiantCropFertilizerID == -1)
        {
            return;
        }
        try
        {
            ModEntry.ModMonitor.Log("Hi!");
            if (__instance.heldObject is not null
                && mineArea >= 120
                && Game1.mine?.GetAdditionalDifficulty() is > 0
                && Game1.random.NextDouble() < 0.05)
            {
                __instance.heldObject.Value = new SObject(ModEntry.GiantCropFertilizerID, 1);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed in postfix for BigSlime's constructor.\n\n{ex}", LogLevel.Error);
        }
    }
}