using AtraCore.Framework.ReflectionManager;
using FastExpressionCompiler.LightExpression;
using HarmonyLib;

namespace AtraShared.Utils.Shims;

public static class JsonAssetsShims
{
    private static Lazy<Func<bool>?> isJAInitialized = new(() =>
    {
        var JA = AccessTools.TypeByName("JsonAssets.Mod");
        if (JA is null)
        {
            return null;
        }

        var inst = Expression.Field(null, JA.GetCachedField("instance", ReflectionCache.FlagTypes.StaticFlags));
        var isInit = Expression.Field(inst, JA.GetCachedField("DidInit", ReflectionCache.FlagTypes.InstanceFlags));

        return Expression.Lambda<Func<bool>>(isInit).CompileFast();
    });

    public static Func<bool>? IsJaInitialized => isJAInitialized.Value;
}
