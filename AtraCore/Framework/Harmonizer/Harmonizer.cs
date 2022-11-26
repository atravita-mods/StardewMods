using System.Collections.Immutable;

#if DEBUG
using System.Diagnostics;
#endif

using System.Reflection;

using HarmonyLib;

namespace AtraCore.Framework.Harmonizer;
public class Harmonizer
{
    private readonly IReadOnlyCollection<string>? excludedCategories;
    private readonly IMonitor logger;

    private readonly Dictionary<string, Harmony> cache = new();

    public Harmonizer(IMonitor logger, string uniqueID)
        : this(logger, uniqueID, null) { }

    public Harmonizer(IMonitor logger, string uniqueID, IEnumerable<string>? excludedCategories)
    {
        this.logger = logger;
        this.UniqueID = uniqueID;

        this.excludedCategories = excludedCategories?.ToImmutableArray();
    }

    internal string UniqueID { get; init; }

    public void PatchAll(Assembly assembly)
    {
#if DEBUG
        Stopwatch sw = Stopwatch.StartNew();
#endif
        foreach (var type in assembly.GetTypes())
        {
            if (type.GetCustomAttribute<HarmonyPatch>() is not HarmonyPatch patch)
            {
                continue;
            }
        }

#if DEBUG
        sw.Stop();
        this.logger.Log($"Took {sw.ElapsedMilliseconds} to apply harmony patches");
#endif
    }
}
