using AtraShared.ConstantsAndEnums;

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
    [HarmonyPatch("GetOneCopyFrom")]
    private static void Postfix(Clothing __instance, Item source)
    {
        if (source is Clothing original)
        {
            __instance.isPrismatic.Value = original.isPrismatic.Value;
        }
    }
}
