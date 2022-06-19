using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace MoreFertilizers.HarmonyPatches.Compat;

/// <summary>
/// Holds patches against radioactive tools and prismatic tools.
/// For some inexplicable reason, both these mods copy the vanilla
/// scarecrows function in a prefix that's quite out of date.
/// </summary>
/// <remarks>This is why atra doesn't like prefixing false unnecessarily, guys.</remarks>
internal static class ExtendedToolsMods
{
    internal static void ApplyPatches(Harmony harmony)
    {
        HarmonyMethod prefix = new(typeof(ExtendedToolsMods), nameof(Prefix));
        Type prismaticPatches = AccessTools.TypeByName("PrismaticTools.Framework.PrismaticPatches");
        MethodInfo prismatcPrefix = AccessTools.Method(prismaticPatches, "Farm_AddCrows");

        if (prismatcPrefix is not null)
        {
            harmony.Patch(prismatcPrefix, prefix: prefix);
            ModEntry.ModMonitor.Log("Found Prismatic Tools, patching for compat", LogLevel.Info);
        }

        Type radioactivePatches = AccessTools.TypeByName("RadioactiveTools.Framework.RadioactivePatches");
        MethodInfo radioactivePrefix = AccessTools.Method(radioactivePatches, "Farm_AddCrows");

        if (radioactivePrefix is not null)
        {
            harmony.Patch(radioactivePrefix, prefix: prefix);
            ModEntry.ModMonitor.Log("Found Radioactive Tools, patching for compat", LogLevel.Info);
        }
    }

    private static bool Prefix(ref bool __result)
    {
        ModEntry.ModMonitor.Log("Disabling addCrows prefix for Prismatic Tools and Radioactive tools");
        __result = true;
        return false;
    }
}
