using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MiniAtraShared.Extensions;

using StardewValley.Objects;

namespace LessMiniShippingBin;

/// <summary>
/// Patches against StardewValley.Objects.Chest.
/// </summary>
[HarmonyPatch(typeof(Chest))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Named for Harmony")]
internal static class ChestPatches
{
    #region delegates

    private static readonly Lazy<AccessTools.FieldRef<Chest, int>> _currentLidFrameGetter = new(() =>
    {
        return AccessTools.FieldRefAccess<Chest, int>("currentLidFrame");
    });

    #endregion

    /// <summary>
    /// Postfix against the chest capacity.
    /// </summary>
    /// <param name="__instance">The chest to look at.</param>
    /// <param name="__result">The requested size of the chest.</param>
    [HarmonyPostfix]
    [HarmonyPriority(Priority.HigherThanNormal)]
    [HarmonyPatch(nameof(Chest.GetActualCapacity))]
    private static void PostfixActualCapacity(Chest __instance, ref int __result)
    {
        try
        {
            __result = __instance.SpecialChestType switch
            {
                Chest.SpecialChestTypes.MiniShippingBin => ModEntry.Config.MiniShippingCapacity,
                // Chest.SpecialChestTypes.Enricher => 9,
                Chest.SpecialChestTypes.JunimoChest => ModEntry.Config.JuminoCapacity,
                Chest.SpecialChestTypes.BigChest => ModEntry.Config.BigChestCapacity,
                Chest.SpecialChestTypes.None => ModEntry.Config.SmallChestCapacity,
                _ => __result
            };
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"overwriting {__instance.SpecialChestType} capacity", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Chest.draw), [typeof(SpriteBatch), typeof(int), typeof(int), typeof(float)])]
    private static void PostfixDraw(Chest __instance, SpriteBatch spriteBatch, int x, int y, float alpha)
    {
        if ((__instance.fridge.Value ? ModEntry.Config.DrawFirstItemFridge : ModEntry.Config.DrawFirstItem)
            && !__instance.giftbox.Value && __instance.playerChest.Value && !__instance.localKickStartTile.HasValue
            && __instance.Items.Count > 0 && __instance.Items[0] is Item itemToDraw)
        {
            int startframe = __instance.startingLidFrame.Value;
            int currentFrame = _currentLidFrameGetter.Value(__instance);
            float alphaAdjustment = __instance.lidFrameCount.Value > 1
                ? 1 - ((currentFrame - startframe) / (float)(__instance.lidFrameCount.Value - 1))
                : 1;

            itemToDraw.drawInMenu(
                spriteBatch,
                location: Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, (y * 64) - 40)),
                scaleSize: 1,
                transparency: alpha * alphaAdjustment,
                layerDepth: MathF.BitIncrement(Math.Max(0f, ((y * 64f) + 80f) / 10000f)),
                drawStackNumber: StackDrawType.Hide);
        }
    }
}