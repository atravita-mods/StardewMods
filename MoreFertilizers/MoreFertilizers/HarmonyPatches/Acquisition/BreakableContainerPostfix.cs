using HarmonyLib;
using Netcode;
using StardewValley.Locations;
using StardewValley.Objects;

namespace MoreFertilizers.HarmonyPatches.Acquisition;

/// <summary>
/// Postfix to add fertilizers to breakable barrels in the mines.
/// </summary>
[HarmonyPatch(typeof(BreakableContainer))]
internal static class BreakableContainerPostfix
{
    [HarmonyPatch(nameof(BreakableContainer.releaseContents))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention")]
    private static void Postfix(GameLocation location, BreakableContainer __instance, NetInt ___containerType)
    {
        if (Game1.random.NextDouble() > 0.01)
        {
            return;
        }
        int objectID = ___containerType.Value switch
        {
            BreakableContainer.barrel => Game1.random.NextDouble() < 0.5
                ? ModEntry.LuckyFertilizerID
                : ModEntry.PaddyCropFertilizerID,
            BreakableContainer.frostBarrel => location is MineShaft shaft && shaft.GetAdditionalDifficulty() > 0
                ? ModEntry.DeluxeFruitTreeFertilizerID
                : ModEntry.OrganicFertilizerID,
            BreakableContainer.darkBarrel => location is MineShaft shaft && shaft.GetAdditionalDifficulty() > 0
                ? ModEntry.DeluxeJojaFertilizerID
                : ModEntry.JojaFertilizerID,
            BreakableContainer.desertBarrel => Game1.random.NextDouble() < 0.5
                ? ModEntry.BountifulFertilizerID
                : ModEntry.FruitTreeFertilizerID,
            BreakableContainer.volcanoBarrel => Game1.random.NextDouble() < 0.5
                ? ModEntry.FishFoodID
                : ModEntry.DomesticatedFishFoodID,
            _ => ModEntry.LuckyFertilizerID,
        };
        Game1.createMultipleObjectDebris(
            index: objectID,
            xTile: (int)__instance.TileLocation.X,
            yTile: (int)__instance.TileLocation.Y,
            number: Game1.random.Next(1, Math.Clamp(Game1.player.MiningLevel / 2, 2, 10)),
            location: location);
    }
}