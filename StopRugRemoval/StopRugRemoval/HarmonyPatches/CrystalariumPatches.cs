using AtraShared.ConstantsAndEnums;

using HarmonyLib;

using StopRugRemoval.Configuration;

namespace StopRugRemoval.HarmonyPatches;

/// <summary>
/// Patches to adjust crystalarium behavior.
/// </summary>
[HarmonyPatch(typeof(SObject))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class CrystalariumPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(SObject.performObjectDropInAction))]
    private static bool Prefix(SObject __instance, bool probe, Farmer who, out SObject? __state)
    {
        __state = null;
        if (!ModEntry.Config.Enabled || ModEntry.Config.CrystalariumBehavior == CrystalariumBehavior.Vanilla
            || __instance.heldObject.Value is null || __instance.heldObject.Value.ParentSheetIndex == __instance.ParentSheetIndex
            || __instance.Name != "Crystalarium")
        {
            return true;
        }

        switch (ModEntry.Config.CrystalariumBehavior)
        {
            case CrystalariumBehavior.Break:
                if (!probe)
                {
                    Game1.showRedMessage(I18n.Crystalarium_RequireBreak());
                }
                return false;
            case CrystalariumBehavior.Keybind:
                if (!ModEntry.Config.FurniturePlacementKey.IsDown())
                {
                    if (!probe)
                    {
                        Game1.showRedMessage(I18n.Crystalarium_RequireKeybind(ModEntry.Config.FurniturePlacementKey));
                    }
                    return false;
                }
                goto case CrystalariumBehavior.Swap;
            case CrystalariumBehavior.Swap:
                if (!who.couldInventoryAcceptThisItem(__instance.heldObject.Value))
                {
                    I18n.Crystalarium_InventoryFull();
                    return false;
                }
                __state = __instance.heldObject.Value;
                return true;
            default:
                return true;
        }
    }

    [HarmonyPatch(nameof(SObject.performObjectDropInAction))]
    private static void Postfix(Farmer who, SObject? __state, bool __result)
    {
        if (__result && __state is not null)
        {
            who.addItemByMenuIfNecessary(__state);
        }
    }
}
