using HarmonyLib;

using StardewValley.Objects;

namespace StopRugRemoval.HarmonyPatches;

/// <summary>
/// Patches on signs.
/// </summary>
[HarmonyPatch(typeof(Sign))]
internal static class SighPatches
{
    [HarmonyPatch(nameof(Sign.checkForAction))]
    private static bool Prefix(bool justCheckingForActivity)
    {
        switch (ModEntry.Config.SignBehavior)
        {
            case Configuration.SignBehavior.Break:
                if (!justCheckingForActivity)
                {
                    Game1.showRedMessage(I18n.Sign_RequireBreak());
                }

                return false;
            case Configuration.SignBehavior.Keybind:
                if (!ModEntry.Config.FurniturePlacementKey.IsDown())
                {
                    if (!justCheckingForActivity)
                    {
                        Game1.showRedMessage(I18n.Sign_RequireKeybind(ModEntry.Config.FurniturePlacementKey));
                    }
                    return false;
                }
                return true;
            default:
                return true;
        }
    }
}
