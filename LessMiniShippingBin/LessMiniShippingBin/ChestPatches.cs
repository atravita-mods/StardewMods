using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.Objects;

namespace LessMiniShippingBin;

/// <summary>
/// Patches against StardewValley.Objects.Chest.
/// </summary>
[HarmonyPatch(typeof(Chest))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention")]
internal class ChestPatches
{
    /// <summary>
    /// Postfix against the chest capacity.
    /// </summary>
    /// <param name="__instance">The chest to look at.</param>
    /// <param name="__result">The requested size of the chest.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Chest.GetActualCapacity))]
    private static void PostfixActualCapacity(Chest __instance, ref int __result)
    {
        try
        {
            switch (__instance.SpecialChestType)
            {
                case Chest.SpecialChestTypes.MiniShippingBin:
                    __result = ModEntry.Config.MiniShippingCapacity;
                    break;
                case Chest.SpecialChestTypes.JunimoChest:
                    __result = ModEntry.Config.JuminoCapacity;
                    break;
                default:
                    // do nothing.
                    break;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed in overwriting {__instance.SpecialChestType} capacity\n\n{ex}", LogLevel.Error);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Chest.draw), new[] {typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) })]
    private static void PostfixDraw(Chest __instance, SpriteBatch spriteBatch, int x, int y, float alpha)
    {
        if ((__instance.fridge.Value ? ModEntry.Config.DrawFirstItemFridge : ModEntry.Config.DrawFirstItem)
            && !__instance.giftbox.Value && __instance.playerChest.Value && !__instance.localKickStartTile.HasValue
            && __instance.items.Count > 0 && __instance.items[0] is Item itemToDraw)
        {
            itemToDraw.drawInMenu(
                spriteBatch,
                location: Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, (y * 64) - 40)),
                scaleSize: 1,
                transparency: alpha,
                layerDepth: Math.Max(0f, ((y * 64f) + 80f) / 10000f),
                drawStackNumber: StackDrawType.Hide);
        }
    }
}