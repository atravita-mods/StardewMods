using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using StardewValley.Objects;

namespace MoreFertilizers.HarmonyPatches.Acquisition;

/// <summary>
/// Holds patches against GameLocation so monsters on farm drop fertilizer.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
internal static class GameLocationPatches
{
    [HarmonyPatch(nameof(GameLocation.monsterDrop))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention")]
    private static void Postfix(GameLocation __instance, Monster monster, int x, int y, Farmer who)
    {
        if(__instance is not Farm || who is null || Game1.random.NextDouble() > 0.75)
        {
            return;
        }

        try
        {
            int passes = 1;
            do
            {
                int fertilizerToDrop = Game1.random.Next(who.combatLevel.Value + 1) switch
                {
                    0 => ModEntry.LuckyFertilizerID,
                    1 => ModEntry.JojaFertilizerID,
                    2 => ModEntry.PaddyCropFertilizerID,
                    3 => ModEntry.OrganicFertilizerID,
                    4 => ModEntry.FruitTreeFertilizerID,
                    5 => ModEntry.FishFoodID,
                    6 => ModEntry.DeluxeFishFoodID,
                    7 => ModEntry.DomesticatedFishFoodID,
                    8 => ModEntry.DeluxeJojaFertilizerID,
                    9 => ModEntry.DeluxeFruitTreeFertilizerID,
                    _ => ModEntry.BountifulFertilizerID,
                };
                if (fertilizerToDrop != -1)
                {
                    __instance.debris.Add(
                        monster.ModifyMonsterLoot(
                            new Debris(
                                new SObject(fertilizerToDrop, Game1.random.Next(1, 4)), new Vector2(x, y), who.Position)));
                }
            }
            while(passes-- > 0 && who.isWearingRing(Ring.burglarsRing));
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod failed while adding additional monster drops!\n\n{ex}", LogLevel.Error);
        }
    }
}