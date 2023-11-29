namespace AtraCore.HarmonyPatches.CustomEquipPatches;

using AtraCore.Framework.Models;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.Tools;

/// <summary>
/// Patches against farmer for the equipment fun.
/// </summary>
[HarmonyPatch(typeof(Farmer))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
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

            __instance.leftRing.Value?.OnPlayerHitRing(__instance);
            __instance.rightRing.Value?.OnPlayerHitRing(__instance);

            __instance.hat.Value?.OnPlayerHit(__instance);
            __instance.shirtItem.Value?.OnPlayerHit(__instance);
            __instance.pantsItem.Value?.OnPlayerHit(__instance);
            __instance.boots.Value?.OnPlayerHit(__instance);
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"postfixing player damage", ex);
        }
    }

    private static void OnPlayerHit(this Item item, Farmer player)
    {
        EquipEffects? effect = AssetManager.GetEquipData(item.ItemId)
                                           ?.GetEffect(EquipmentBuffTrigger.OnPlayerHit, player.currentLocation, player);
        if (effect is not null)
        {
            effect.AddBuff(item, player);
            effect.AddRegen(player);
        }
    }

    private static void OnPlayerHitRing(this Ring ring, Farmer player)
    {
        if (ring is CombinedRing combined)
        {
            foreach (Ring? subring in combined.combinedRings)
            {
                subring?.OnPlayerHitRing(player);
            }
        }
        else
        {
            ring.OnPlayerHit(player);
        }

    }
}
