using AtraShared.ConstantsAndEnums;

using HarmonyLib;

using StardewValley.Objects;

namespace StopRugRemoval.HarmonyPatches;

/// <summary>
/// Patches on signs.
/// </summary>
[HarmonyPatch(typeof(Sign))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class SignPatches
{
    [HarmonyPatch(nameof(Sign.checkForAction))]
    private static bool Prefix(Sign __instance, bool justCheckingForActivity)
    {
        if (!ModEntry.Config.Enabled || __instance.displayItem.Value is null)
        {
            return true;
        }
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
