using AtraCore.Framework.ReflectionManager;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.Utils.Shims.JAInternalTypesShims;

using CommunityToolkit.Diagnostics;
using FastExpressionCompiler.LightExpression;
using HarmonyLib;

namespace AtraShared.Utils.Shims;

public static class JsonAssetsShims
{
    private static bool initialized = false;

    // APIs
    private static IJsonAssetsAPI? jsonAssets;

    internal static IJsonAssetsAPI? JsonAssets => jsonAssets;

    private static IEPUConditionsChecker? epu;

    internal static IEPUConditionsChecker? EPU => epu;

    public static void Initialize(IMonitor monitor, ITranslationHelper translation, IModRegistry registry)
    {
        Guard.IsNotNull(monitor);
        Guard.IsNotNull(translation);
        Guard.IsNotNull(registry);

        if (initialized)
        {
            return;
        }

        initialized = true;

        IntegrationHelper integrationHelper = new(monitor, translation, registry, LogLevel.Trace);
        if (integrationHelper.TryGetAPI("spacechase0.JsonAssets", "1.10.6", out jsonAssets)
            && !integrationHelper.TryGetAPI("Cherry.ExpandedPreconditionsUtility", "1.0.1", out epu))
        {
            monitor.Log("JA found but EPU not. EPU conditions will automatically fail.", LogLevel.Info);
        }
    }

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
            var name = CropDataShims.GetSeedName!(crop);
        }

        return ret;
    }

    #region methods

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

    #endregion
}
