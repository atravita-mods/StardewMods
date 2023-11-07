using AtraBase.Toolkit.Extensions;

using AtraShared.ConstantsAndEnums;

using HarmonyLib;

using StardewValley.Locations;
using StardewValley.Objects;

namespace MoreFertilizers.HarmonyPatches.Acquisition;

/// <summary>
/// Postfix to add fertilizers to breakable barrels in the mines.
/// </summary>
[HarmonyPatch(typeof(BreakableContainer))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class BreakableContainerPostfix
{
    [HarmonyPatch(nameof(BreakableContainer.releaseContents))]
    private static void Postfix(GameLocation location, BreakableContainer __instance)
    {
        if (!Random.Shared.OfChance(0.01 + (Game1.player.DailyLuck / 20) + (Math.Min(3, Game1.player.LuckLevel) / 100.0)))
        {
            return;
        }
        int objectID = __instance.ItemId switch
        {
            BreakableContainer.barrelId => location is MineShaft shaft && shaft.GetAdditionalDifficulty() > 0
                ? (Random.Shared.RollDice(3) ? ModEntry.TreeTapperFertilizerID : ModEntry.MiraculousBeveragesID)
                : (Random.Shared.RollDice(2) ? ModEntry.LuckyFertilizerID : ModEntry.PaddyCropFertilizerID),
            BreakableContainer.frostBarrelId => location is MineShaft shaft && shaft.GetAdditionalDifficulty() > 0
                ? (Random.Shared.RollDice(3) ? ModEntry.RapidBushFertilizerID : ModEntry.DeluxeFruitTreeFertilizerID)
                : (Random.Shared.RollDice(2) ? ModEntry.SeedyFertilizerID : ModEntry.WisdomFertilizerID),
            BreakableContainer.darkBarrelId => location is MineShaft shaft && shaft.GetAdditionalDifficulty() > 0
                ? (Utility.hasFinishedJojaRoute() && Random.Shared.RollDice(16) ? ModEntry.SecretJojaFertilizerID : ModEntry.DeluxeJojaFertilizerID)
                : (Random.Shared.RollDice(2) ? ModEntry.JojaFertilizerID : ModEntry.RadioactiveFertilizerID),
            BreakableContainer.desertBarrelId => (location is MineShaft shaft && shaft.GetAdditionalDifficulty() > 0)
                ? (Random.Shared.RollDice(2) ? ModEntry.BountifulFertilizerID : ModEntry.FruitTreeFertilizerID)
                : (Random.Shared.RollDice(3) ? ModEntry.BountifulBushID : ModEntry.OrganicFertilizerID),
            BreakableContainer.volcanoBarrelId =>
                Random.Shared.Next(5) switch
                {
                    0 => ModEntry.FishFoodID,
                    1 => ModEntry.EverlastingFertilizerID,
                    2 => ModEntry.DeluxeFishFoodID,
                    3 => ModEntry.EverlastingFruitTreeFertilizerID,
                    _ => ModEntry.DomesticatedFishFoodID,
                },
            _ => ModEntry.FishFoodID, // should never happen.
        };
        Game1.createMultipleObjectDebris(
            index: objectID,
            xTile: (int)__instance.TileLocation.X,
            yTile: (int)__instance.TileLocation.Y,
            number: Random.Shared.Next(1, Math.Clamp(Game1.player.MiningLevel / 2, 2, 6)),
            location: location);
    }
}