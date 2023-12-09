using AtraBase.Toolkit.Extensions;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Utilities;

using StardewValley.Monsters;
using StardewValley.Objects;

namespace MoreFertilizers.HarmonyPatches.Acquisition;

/// <summary>
/// Holds patches against GameLocation so monsters on farm drop fertilizer.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class GameLocationPatches
{
#pragma warning disable SA1310 // Field names should not contain underscore. Reviewed.
    private const int MIN_MONSTER_HEALTH = 40;
    private const double DEFAULT_DROP_CHANCE = 0.25;
    private static readonly PerScreen<double> DropChance = new(() => DEFAULT_DROP_CHANCE);
#pragma warning restore SA1310 // Field names should not contain underscore

    /// <summary>
    /// Resets the drop chance, once per day.
    /// </summary>
    internal static void Reinitialize() => DropChance.Value = DEFAULT_DROP_CHANCE;

    [HarmonyPatch(nameof(GameLocation.monsterDrop))]
    private static void Postfix(GameLocation __instance, Monster monster, int x, int y, Farmer who)
    {
        if(__instance is not Farm || who is null || monster.MaxHealth < MIN_MONSTER_HEALTH
          || !Random.Shared.OfChance((monster is RockGolem ? 2.5 : 1 ) * DropChance.Value))
        {
            return;
        }
        DropChance.Value *= 0.75;

        try
        {
            int passes = 1;
            do
            {
                int fertilizerToDrop = who.combatLevel.Value.GetRandomFertilizerFromLevel();
                if (fertilizerToDrop != -1)
                {
                    __instance.debris.Add(
                        monster.ModifyMonsterLoot(
                            new Debris(
                                item: new SObject(
                                    fertilizerToDrop,
                                    initialStack: Random.Shared.Next(1, Math.Clamp(monster.MaxHealth / MIN_MONSTER_HEALTH, 1, 4))),
                                debrisOrigin: new Vector2(x, y),
                                targetLocation: who.Position)));
                }
            }
            while(passes-- > 0 && who.isWearingRing(Ring.BurglarsRingId));
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding additional monster drops", ex);
        }
    }
}