using System.Reflection;

using AtraCore.Framework.ReflectionManager;

using FastExpressionCompiler.LightExpression;
using HarmonyLib;
using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops.HarmonyPatches.GrassPatches;

internal static class SObjectPatches
{
    /// <summary>
    /// A mod data key used to mark custom grass types.
    /// </summary>
    internal const string ModDataKey = "atravita.GrowableGiantCrop.GrassType";

    /// <summary>
    /// The ParentSheetIndex of a grass starter.
    /// </summary>
    internal const int GrassStarterIndex = 297;

    private static Lazy<Func<int, SObject>?> instantiateMoreGrassStarter = new(() =>
    {
        Type? moreGrass = AccessTools.TypeByName("MoreGrassStarters.GrassStarterItem");
        if (moreGrass is null)
        {
            return null;
        }

        ParameterExpression which = Expression.ParameterOf<int>("which");
        ConstructorInfo constructor = moreGrass.GetCachedConstructor<int>(ReflectionCache.FlagTypes.InstanceFlags);
        NewExpression newObj = Expression.New(constructor, which);
        return Expression.Lambda<Func<int, SObject>>(newObj, which).CompileFast();
    });

    private static Lazy<Func<int, Grass>?> instantiateMoreGrassGrass = new(() =>
    {
        Type? moreGrass = AccessTools.TypeByName("MoreGrassStarters.CustomGrass");
        if (moreGrass is null)
        {
            return null;
        }

        ParameterExpression which = Expression.ParameterOf<int>("which");
        ConstantExpression numberOfWeeds = Expression.ConstantInt(4);
        ConstructorInfo constructor = moreGrass.GetCachedConstructor<int, int>(ReflectionCache.FlagTypes.InstanceFlags);
        NewExpression newObj = Expression.New(constructor, which, numberOfWeeds);
        return Expression.Lambda<Func<int, Grass>>(newObj, which).CompileFast();
    });

    private static bool hasMoreGrassStarters = false;

    internal static void Initialize(IModRegistry registry)
    {
        hasMoreGrassStarters = registry.IsLoaded("spacechase0.MoreGrassStarters");
    }
}
