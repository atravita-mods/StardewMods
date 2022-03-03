#if HARMONY

using System.Reflection;
using System.Text;
using AtraBase.Toolkit.Extensions;
using HarmonyLib;

namespace AtraShared.Utils.Extensions;

/// <summary>
/// Extensions for Harmony.
/// </summary>
internal static class HarmonyExtensions
{
    /// <summary>
    /// Snitch on all the functions patched.
    /// </summary>
    /// <param name="harmony">Harmony instance.</param>
    /// <param name="monitor">Logger.</param>
    /// <param name="uniqueID">Unique ID to look for. Leave null to not filter.</param>
    internal static void Snitch(this Harmony harmony, IMonitor monitor, string? uniqueID = null)
    {
        Func<Patch, bool> filter = uniqueID is null ? (p) => true : (p) => p.owner == uniqueID;

        foreach (MethodBase? method in harmony.GetPatchedMethods())
        {
            if (method is null)
            {
                continue;
            }
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
    }
}

#endif