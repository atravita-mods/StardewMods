using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;

namespace ExperimentalLagReduction.HarmonyPatches.MiniChanges;

[HarmonyPatch(typeof(PathFindController))]
internal static class PathfinderPatches
{
    [HarmonyPatch("getPreferenceValueForTerrainType")]
    private static bool Prefix(GameLocation l, int x, int y, ref int __result)
    {
        Vector2 tile = new(x, y);

        if (l.Objects.TryGetValue(tile, out SObject? obj) && !obj.isPassable())
        {
            __result = 15;
            return false;
        }
        if (l.terrainFeatures.TryGetValue(tile, out TerrainFeature? terrainFeature))
        {
            if (terrainFeature is Flooring)
            {
                __result = -7;
                return false;
            }
            else if (terrainFeature is Grass)
            {
                __result = -5;
                return false;
            }
            else if (terrainFeature is Grass)
            {
                __result = -5;
                return false;
            }
            else if (!terrainFeature.isPassable())
            {
                __result = 15;
                return false;
            }
        }

        string? type = l.doesTileHaveProperty(x, y, "Type", "Back");
        if (type is null || type.Length > 5)
        {
            __result = 0;
            return false;
        }

        // SAFETY: length was checked earlier, caps to 5
        Span<char> lowered = stackalloc char[type.Length + 10];
        int copiedCount = type.AsSpan().ToLowerInvariant(lowered);

        if (copiedCount < 0)
        {
            ModEntry.ModMonitor.LogOnce($"Failed to lowercase {type}, weird.", LogLevel.Warn);
            return true;
        }

        lowered = lowered[..copiedCount];

        __result = lowered switch
        {
            "stone" => -7,
            "wood" => -4,
            "dirt" => -2,
            "grass" => -1,
            _ => 0
        };

        return false;
    }

}
