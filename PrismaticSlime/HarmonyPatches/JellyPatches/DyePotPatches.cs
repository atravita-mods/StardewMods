using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Menus;

// TODO - color in the little slots somehow and make them not interactable.
namespace PrismaticSlime.HarmonyPatches.JellyPatches;

/// <summary>
/// Patches against the dye pots.
/// </summary>
[HarmonyPatch(typeof(DyeMenu))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class DyePotPatches
{
    /// <summary>
    /// The mod data key that denotes when the dye menu should be free.
    /// </summary>
    internal const string ModData = "atravita.PrismaticSlime.DyeMenu";

    [HarmonyPatch(nameof(DyeMenu.CanDye))]
    private static void Postfix(ref bool __result)
    {
        if (!__result && Game1.player.modData.GetInt(ModData) is > 0)
        {
            __result = true;
        }
    }
}
