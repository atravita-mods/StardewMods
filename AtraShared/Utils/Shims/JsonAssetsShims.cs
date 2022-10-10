using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.ReflectionManager;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.Utils.Shims.JAInternalTypesShims;

using CommunityToolkit.Diagnostics;
using FastExpressionCompiler.LightExpression;
using HarmonyLib;

namespace AtraShared.Utils.Shims;

/// <summary>
/// Holds shims against ja.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Fields kept near accessors.")]
public static class JsonAssetsShims
{
    private static bool initialized = false;

    #region APIs
    private static IJsonAssetsAPI? jsonAssets;

    /// <summary>
    /// Gets the JA API, if available.
    /// </summary>
    internal static IJsonAssetsAPI? JsonAssets => jsonAssets;

    private static IEPUConditionsChecker? epu;

    /// <summary>
    /// Gets the EPU API, if available.
    /// </summary>
    internal static IEPUConditionsChecker? EPU => epu;
    #endregion

    /// <summary>
    /// Initializes the shims.
    /// </summary>
    /// <param name="monitor">Monitor instance.</param>
    /// <param name="translation">A translation instance.</param>
    /// <param name="registry">Registry instance.</param>
    public static void Initialize(IMonitor monitor, ITranslationHelper translation, IModRegistry registry)
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        Guard.IsNotNull(monitor);
        Guard.IsNotNull(translation);
        Guard.IsNotNull(registry);

        IntegrationHelper integrationHelper = new(monitor, translation, registry, LogLevel.Trace);
        if (integrationHelper.TryGetAPI("spacechase0.JsonAssets", "1.10.6", out jsonAssets)
            && !integrationHelper.TryGetAPI("Cherry.ExpandedPreconditionsUtility", "1.0.1", out epu))
        {
            monitor.Log("ja found but EPU not. EPU conditions will automatically fail.", LogLevel.Info);
        }
    }

    /// <summary>
    /// Checks to see if an event precondition requires EPU. A condition requires EPU if it starts with ! or is longer than two letters.
    /// </summary>
    /// <param name="condition">Condition to check.</param>
    /// <returns>True if EPU is required.</returns>
    public static bool ConditionRequiresEPU(ReadOnlySpan<char> condition)
        => condition[0] == '!' || condition.GetIndexOfWhiteSpace() > 3;

    private static Lazy<Dictionary<string, string>?> JACropCache = new(SetUpJAIntegration);

    private static Dictionary<string, string>? SetUpJAIntegration()
    {
        var ja = AccessTools.TypeByName("JsonAssets.Mod");
        if (ja is null)
        {
            return null;
        }

        var inst = ja.GetCachedField("instance", ReflectionCache.FlagTypes.StaticFlags).GetValue(null);
        var cropdata = ja.GetCachedField("Crops", ReflectionCache.FlagTypes.InstanceFlags).GetValue(inst) as IList<object>;

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

    private static readonly Lazy<Func<bool>?> isJAInitialized = new(() =>
    {
        var ja = AccessTools.TypeByName("JsonAssets.Mod");
        if (ja is null)
        {
            return null;
        }

        var inst = Expression.Field(null, ja.GetCachedField("instance", ReflectionCache.FlagTypes.StaticFlags));
        var isInit = Expression.Field(inst, ja.GetCachedField("DidInit", ReflectionCache.FlagTypes.InstanceFlags));

        return Expression.Lambda<Func<bool>>(isInit).CompileFast();
    });

    /// <summary>
    /// Gets a delegate that checks whether JA is initialized or not.
    /// </summary>
    public static Func<bool>? IsJaInitialized => isJAInitialized.Value;

    #endregion
}
