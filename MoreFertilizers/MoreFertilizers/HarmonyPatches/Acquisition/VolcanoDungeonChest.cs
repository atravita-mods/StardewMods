using AtraBase.Toolkit.Extensions;

using HarmonyLib;
using Netcode;
using StardewValley.Locations;

namespace MoreFertilizers.HarmonyPatches.Acquisition;

/// <summary>
/// Postfixes Volcano Dungeon Chest to spawn fertilizers in there.
/// </summary>
[HarmonyPatch(typeof(VolcanoDungeon))]
internal static class VolcanoDungeonChest
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPatch(nameof(VolcanoDungeon.PopulateChest))]
    private static void PostfixPopulateChest(NetObjectList<Item> items, Random chest_random, int chest_type)
    {
        if (chest_random.RollDice(3))
        {
            int fertilizerToDrop = Game1.player.miningLevel.Value.GetRandomFertilizerFromLevel();
            if (fertilizerToDrop != -1)
            {
                items.Add(new SObject(fertilizerToDrop, Game1.random.Next(1, 4)));
            }
        }
        else if (chest_type == 1 && chest_random.OfChance(0.4))
        {
            if (ModEntry.MiraculousBeveragesID != -1)
            {
                items.Add(new SObject(ModEntry.MiraculousBeveragesID, Game1.random.Next(2, 5)));
            }
        }
        else if (chest_random.OfChance(0.5))
        {
            if (ModEntry.SeedyFertilizerID != -1)
            {
                items.Add(new SObject(ModEntry.SeedyFertilizerID, Game1.random.Next(2, 5)));
            }
        }

        if (ModEntry.SecretJojaFertilizerID != -1 && chest_random.RollDice(1024))
        {
            items.Add(new SObject(ModEntry.SecretJojaFertilizerID, 1));
        }
    }
}