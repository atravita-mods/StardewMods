using AtraCore.Framework.ReflectionManager;
using FastExpressionCompiler.LightExpression;
using HarmonyLib;

namespace AtraShared.Utils.Shims;

public static class JsonAssetsShims
{
    private static Lazy<Dictionary<string, string>?> JACropCache = new(SetUpJAIntegration);

    private static Dictionary<string, string>? SetUpJAIntegration()
    {
        var JA = AccessTools.TypeByName("JsonAssets.Mod");
        if (JA is null)
        {
            return null;
        }

        var inst = JA.GetCachedField("instance", ReflectionCache.FlagTypes.StaticFlags).GetValue(null);
        var cropdata = JA.GetCachedField("Crops", ReflectionCache.FlagTypes.InstanceFlags).GetValue(inst) as IList<object>;

        if (cropdata is null)
        {
            return null;
        }

        Dictionary<string, string> ret = new();

        foreach (var crop in cropdata)
        {
        }

        return ret;
    }

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
