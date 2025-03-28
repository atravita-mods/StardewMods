using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using MiniAtraShared.Extensions;

using StardewValley.SpecialOrders;
using StardewValley.SpecialOrders.Objectives;

namespace SlightlyMoreDehardcoding.HarmonyPatches;
internal static class ObjectivePatch
{
    internal static void Apply(Harmony harmony)
    {
        try
        {
            var objectives = typeof(OrderObjective);
            var patch = new HarmonyMethod(typeof(ObjectivePatch), nameof(Postfix));

            foreach (var assembly in AccessTools.AllAssemblies())
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }

                foreach (var type in AccessTools.GetTypesFromAssembly(assembly))
                {
                    if (type.IsAbstract || type.IsInterface)
                    {
                        continue;
                    }

                    if (type.IsAssignableTo(objectives)
                        && AccessTools.DeclaredMethod(type, "Load", [typeof(SpecialOrder), typeof(Dictionary<string, string>)]) is { } method)
                    {
                        harmony.Patch(method, postfix: patch);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("applying special order patches.", ex);
        }
    }

    private static void Postfix(OrderObjective __instance, Dictionary<string, string> data)
    {
        if (data?.TryGetValue("FailOnCompletion", out var v) == true
            && bool.TryParse(v, out var value) && value)
        {
            __instance.failOnCompletion.Value = true;
        }
    }
}
