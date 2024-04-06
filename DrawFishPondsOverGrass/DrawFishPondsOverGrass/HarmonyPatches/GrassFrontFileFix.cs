using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.TerrainFeatures;


namespace DrawFishPondsOverGrass.HarmonyPatches;

[HarmonyPatch(typeof(Grass))]
internal static class GrassFrontFileFix
{
    private static Lazy<Func<Grass, int[]>> _getOffset2 = new(()
        => typeof(Grass).GetCachedField("offset2", ReflectionCache.FlagTypes.InstanceFlags)
                       .GetInstanceFieldGetter<Grass, int[]>());

    private static Lazy<Func<Grass, int[]>> _getOffset4 = new(()
    => typeof(Grass).GetCachedField("offset4", ReflectionCache.FlagTypes.InstanceFlags)
                   .GetInstanceFieldGetter<Grass, int[]>());

    [HarmonyPatch(nameof(Grass.setUpRandom))]
    private static void Postfix(Grass __instance)
    {
        var tile = __instance.Tile.ToPoint();
        var location = __instance.Location;

        if (location?.Map?.GetLayer("Front")?.PickTile(new(tile.X * Game1.tileSize, tile.Y * Game1.tileSize), Game1.viewport.Size) is null)
        {
            return;
        }

        try
        {
            AdjustedGrass(_getOffset2.Value(__instance));
            AdjustedGrass(_getOffset4.Value(__instance));
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adjust grass draw", ex);
        }
    }

    private static void AdjustedGrass(int[] offset)
    {
        for (int i = 0; i < offset.Length; i++)
        {
            offset[i] = Random.Shared.Next(-2, 1);
        }
    }
}
