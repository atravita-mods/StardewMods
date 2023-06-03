using HarmonyLib;

using StardewValley.Objects;

namespace PrismaticClothing.HarmonyPatches;

/// <summary>
/// Override Clothing.getOne to preserve Prismatic-ness.
/// </summary>
[HarmonyPatch(typeof(Clothing))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class ClothingPatches
{
    [HarmonyPatch(nameof(Clothing.getOne))]
    private static void Postfix(Clothing __instance, Item __result)
    {
        if (__result is Clothing result)
        {
            result.isPrismatic.Value = __instance.isPrismatic.Value;
        }
    }
}
