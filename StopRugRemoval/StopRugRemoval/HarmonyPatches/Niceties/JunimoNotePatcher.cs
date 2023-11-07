using AtraShared.ConstantsAndEnums;

using HarmonyLib;

using Microsoft.Xna.Framework.Input;

using StardewValley.Menus;

namespace StopRugRemoval.HarmonyPatches.Niceties;

/// <summary>
/// Patches the jumino menu so paging can be done with arrow keys.
/// </summary>
[HarmonyPatch(typeof(JunimoNoteMenu))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class JunimoNotePatcher
{
    [HarmonyPatch(nameof(JunimoNoteMenu.receiveKeyPress))]
    private static void Postfix(JunimoNoteMenu __instance, Keys key, bool ___specificBundlePage)
    {
        if (__instance.fromGameMenu && !___specificBundlePage)
        {
            switch (key)
            {
                case Keys.Left:
                    __instance.SwapPage(-1);
                    break;
                case Keys.Right:
                    __instance.SwapPage(1);
                    break;
            }
        }
    }
}
