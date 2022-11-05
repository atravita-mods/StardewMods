using System.Text;

using AtraBase.Toolkit;
using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.Reflection;
using AtraBase.Toolkit.StringHandler;

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
    private const int eventID = int.MinValue + 4993;

    private static bool initialized = false;

    private static IMonitor modMonitor = null!;

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
    /// <param name="monitor">modMonitor instance.</param>
    /// <param name="translation">A translation instance.</param>
    /// <param name="registry">Registry instance.</param>
    public static void Initialize(IMonitor monitor, ITranslationHelper translation, IModRegistry registry)
    {
        if (initialized)
        {
            return;
        }

        Guard.IsNotNull(monitor);
        Guard.IsNotNull(translation);
        Guard.IsNotNull(registry);

        modMonitor = monitor;

        IntegrationHelper integrationHelper = new(monitor, translation, registry, LogLevel.Trace);
        if (integrationHelper.TryGetAPI("spacechase0.JsonAssets", "1.10.6", out jsonAssets)
            && !integrationHelper.TryGetAPI("Cherry.ExpandedPreconditionsUtility", "1.0.1", out epu))
        {
            monitor.Log("ja found but EPU not. EPU conditions will automatically fail.", LogLevel.Info);
        }
        epu?.Initialize(false, registry.ModID);

        initialized = true;
    }

    /// <summary>
    /// Checks to see if an event precondition requires EPU. A condition requires EPU if it starts with ! or is longer than two letters.
    /// </summary>
    /// <param name="condition">Condition to check.</param>
    /// <returns>True if EPU is required.</returns>
    public static bool ConditionRequiresEPU(ReadOnlySpan<char> condition)
        => condition[0] == '!' || condition.GetIndexOfWhiteSpace() > 3;

    public static bool IsAvailableSeed(string name)
    {
        Guard.IsNotNullOrWhiteSpace(name);
        if(JACropCache?.TryGetValue(name, out string? conditions) != true)
        {
            return false;
        }
        if(string.IsNullOrWhiteSpace(conditions))
        {
            return true;
        }
        if(epu is not null)
        {
            return epu.CheckConditions(conditions);
        }
        Farm farm = Game1.getFarm();
        bool replace = Game1.player.eventsSeen.Remove(eventID);
        bool ret = farm.checkEventPrecondition($"{eventID}/{conditions}") > 0;
        if (replace)
        {
            Game1.player.eventsSeen.Add(eventID);
        }
        return ret;
    }

    private static Lazy<Dictionary<string, string>?> jaCropCache = new(SetUpJAIntegration);

    /// <summary>
    /// Gets a name->preconditions map of JA crops, or null if JA was not installed/reflection failed.
    /// </summary>
    public static Dictionary<string, string>? JACropCache => jaCropCache.Value;

    private static Dictionary<string, string>? SetUpJAIntegration()
    {
        var ja = AccessTools.TypeByName("JsonAssets.Mod");
        if (ja is null)
        {
            return null;
        }

        var inst = ja.StaticFieldNamed("instance").GetValue(null);
        var cropdata = ja.InstanceFieldNamed("Crops").GetValue(inst) as IList<object>;

        if (cropdata is null)
        {
            return null;
        }

        Dictionary<string, string> ret = new();

        foreach (var crop in cropdata)
        {
            var name = CropDataShims.GetSeedName!(crop);
            if (name is null)
            {
                continue;
            }

            var requirements = CropDataShims.GetSeedRestrictions!(crop);
            if (requirements is null)
            {
                ret[name!] = string.Empty; // no conditions
                continue;
            }

            StringBuilder sb = StringBuilderCache.Acquire(64);

            foreach (var requirement in requirements)
            {
                if (requirement is not null)
                {
                    if (ConditionRequiresEPU(requirement) && EPU is null)
                    {
                        modMonitor.Log($"{requirement} requires EPU, which is not isntalled", LogLevel.Warn);
                        sb.Clear();
                        StringBuilderCache.Release(sb);
                        goto breakcontinue;
                    }

                    sb.Append(requirement).Append('/');
                }
            }

            if (sb.Length > 0)
            {
                ret[name!] = sb.ToString(0, sb.Length - 1);
            }

            StringBuilderCache.Release(sb);
breakcontinue:
            ;
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
