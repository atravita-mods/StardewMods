using AtraBase.Toolkit.Extensions;

using AtraShared.ConstantsAndEnums;

using HarmonyLib;

using StardewValley.Monsters;

namespace PrismaticSlime.HarmonyPatches.EggPatches;

/// <summary>
/// Adds the prismatic slime egg as a possible drop to prismatic slimes.
/// </summary>
[HarmonyPatch(typeof(GreenSlime))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class PostfixGreenSlime
{
    [UsedImplicitly]
    [HarmonyPatch(nameof(GreenSlime.getExtraDropItems))]
    private static void Postfix(GreenSlime __instance,  List<Item> __result)
    {
        if (__instance.prismatic.Value
            && Random.Shared.OfChance(0.5))
        {
            __result.Add(new SObject(ModEntry.PrismaticSlimeEgg, 1));
        }
    }
}
