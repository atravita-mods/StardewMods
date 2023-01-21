using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Menus;

// TODO - color in the little slots somehow and make them not interactable.

namespace PrismaticSlime.HarmonyPatches.JellyPatches;
[HarmonyPatch(typeof(DyeMenu))]
internal static class DyePotPatches
{
    internal const string ModData = "atravita.PrismaticSlime.DyeMenu";

    [HarmonyPatch(nameof(DyeMenu.CanDye))]
    private static void Postfix(ref bool __result)
    {
        if (!__result)
        {
            if (Game1.player.modData.GetInt(ModData) is > 0)
            {
                __result = true;
            }
        }
    }
}
