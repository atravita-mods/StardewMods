namespace AtraCore.HarmonyPatches.CustomRingPatches;

using AtraCore.Framework.Models;

using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.Tools;

[HarmonyPatch(typeof(Farmer))]
internal static class FarmerPatches
{
    [HarmonyPatch(nameof(Farmer.takeDamage))]
    private static void Postfix(Farmer __instance, int damage, bool overrideParry, Monster damager, bool __runOriginal)
    {
        try
        {
            if (Game1.eventUp || __instance.FarmerSprite.isPassingOut())
            {
                return;
            }

            if (damage <= 0)
            {
                return;
            }

            if (!__runOriginal)
            {
                return;
            }

            if (!overrideParry && damager?.isInvincible() == true)
            {
                return;
            }

            if (!__instance.CanBeDamaged() || (__instance.CurrentTool is MeleeWeapon sword && sword.isOnSpecial && sword.type.Value == 3))
            {
                return;
            }

            if (__instance.isWearingRing("520") && damager is GreenSlime or BigSlime)
            {
                return;
            }

            __instance.leftRing.Value?.OnPlayerHit(__instance);
            __instance.rightRing.Value?.OnPlayerHit(__instance);
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"postfixing player damage", ex);
        }
    }

    private static void OnPlayerHit(this Ring ring, Farmer player)
    {
        RingEffects? effect = AssetManager.GetRingData(ring.ItemId)?.GetEffect(RingBuffTrigger.OnPlayerHit, player.currentLocation, player);
        effect?.AddBuff(ring, player);
    }
}
