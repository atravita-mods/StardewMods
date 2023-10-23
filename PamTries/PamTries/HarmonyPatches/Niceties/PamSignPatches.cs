using HarmonyLib;

using Microsoft.Xna.Framework;

namespace PamTries.HarmonyPatches.Niceties;

[HarmonyPatch(typeof(Event))]
internal static class PamSignPatches
{
    [HarmonyPatch("addSpecificTemporarySprite")]
    private static void Postfix(string key)
    {
        if (key == "pamYobaStatue" && Game1.getLocationFromName("Trailer_Big")?.Objects?.TryGetValue(new Vector2(26f, 9f), out SObject? sign) == true
            && sign.bigCraftable.Value && sign.ParentSheetIndex == 34)
        {
            ModEntry.ModMonitor.Log($"Preventing players from stealing Pam's Yoba shrine.");
            sign.Fragility = SObject.fragility_Indestructable;
        }
    }
}
