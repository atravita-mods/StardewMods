#if HARMONY

using System.Reflection;
using System.Text;
using AtraBase.Toolkit.Extensions;
using HarmonyLib;

namespace AtraShared.Utils.Extensions;

/// <summary>
/// Extensions for Harmony.
/// </summary>
public static class HarmonyExtensions
{
    /// <summary>
    /// Snitch on all the functions patched.
    /// </summary>
    /// <param name="harmony">Harmony instance.</param>
    /// <param name="monitor">Logger.</param>
    /// <param name="uniqueID">Unique ID to look for. Leave null to not filter.</param>
    public static void Snitch(this Harmony harmony, IMonitor monitor, string? uniqueID = null)
    {
        Func<Patch, bool> filter = uniqueID is null ? (_) => true : (p) => p.owner == uniqueID;

        foreach (MethodBase? method in harmony.GetPatchedMethods())
        {
            if (method is null)
            {
                continue;
            }
            method.Snitch(monitor, filter);
        }
    }

    /// <summary>
    /// Snitch on patches from a single function.
    /// </summary>
    /// <param name="method">Method to look at.</param>
    /// <param name="monitor">Logger.</param>
    /// <param name="filter">Filter. Leave null to not filter.</param>
    public static void Snitch(this MethodBase method, IMonitor monitor, Func<Patch, bool>? filter = null)
    {
        filter ??= (_) => true;
        Patches patches = Harmony.GetPatchInfo(method);

        StringBuilder sb = new();
        sb.Append("Patched method ").Append(method.FullDescription());
        foreach (Patch patch in patches.Prefixes.Where(filter))
        {
            sb.AppendLine().Append("\tPrefixed with method: ").Append(patch.PatchMethod.GetFullName());
        }
        foreach (Patch patch in patches.Postfixes.Where(filter))
        {
            sb.AppendLine().Append("\tPostfixed with method: ").Append(patch.PatchMethod.GetFullName());
        }
        foreach (Patch patch in patches.Transpilers.Where(filter))
        {
            sb.AppendLine().Append("\tTranspiled with method: ").Append(patch.PatchMethod.GetFullName());
        }
        foreach (Patch patch in patches.Finalizers.Where(filter))
        {
            sb.AppendLine().Append("\tFinalized with method: ").Append(patch.PatchMethod.GetFullName());
        }
        monitor.Log(sb.ToString(), LogLevel.Trace);
    }

    /// <summary>
    /// Snitch on patches from a single function.
    /// </summary>
    /// <param name="method">Method to look at.</param>
    /// <param name="monitor">Logger.</param>
    /// <param name="uniqueID">UniqueID to filter for.</param>
    public static void Snitch(this MethodBase method, IMonitor monitor, string uniqueID)
        => method.Snitch(monitor, (Patch p) => p.owner == uniqueID);
}

#endif