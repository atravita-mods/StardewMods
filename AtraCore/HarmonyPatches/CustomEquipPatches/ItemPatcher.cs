// #define TRACELOG

namespace AtraCore.HarmonyPatches.CustomEquipPatches;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using AtraBase.Toolkit;
using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.Models;
using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

using StardewValley;
using StardewValley.Buffs;
using StardewValley.Locations;
using StardewValley.Objects;

/// <summary>
/// Holds patches for custom buffs on clothing.
/// </summary>
[HarmonyPatch]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Preference.")]
internal static class ItemPatcher
{
    private const string LightKey = "atravita.EquipLight";

    // maps the ring ID to the current effect of the ring for tooltips
    private static readonly PerScreen<Dictionary<string, EquipEffects>> _tooltipMap = new(static () => new());

    // maps the items to their active effects
    private static readonly PerScreen<ConditionalWeakTable<Item, EquipEffects>> _activeEffects = new(static () => new());

    // holds tooltip cache for combined rings
    private static readonly ConditionalWeakTable<CombinedRing, EquipEffects?> _combinedTooltips = [];

    // holds tooltip cache for boots
    private static readonly ConditionalWeakTable<Boots, EquipEffects?> _bootsTooltips = [];

    // holds references to active lights
    private static readonly PerScreen<Dictionary<Item, int>> _lightSources = new(static () => new());

    // holds the remainder of health from the last round of health updates.
    private static readonly PerScreen<float> _healthRemainder = new(static () => 0f);

    /// <summary>
    /// Gets or sets a value indicating the "remainder" of health not yet granted to the player.
    /// </summary>
    internal static float HealthRemainder
    {
        get => _healthRemainder.Value;
        set => _healthRemainder.Value = value;
    }

    #region delegates
    private static readonly Lazy<Func<Ring, int?>> lightIDSourceGetter = new(() =>
        typeof(Ring).GetCachedField("_lightSourceID", ReflectionCache.FlagTypes.InstanceFlags)
                    .GetInstanceFieldGetter<Ring, int?>());

    private static readonly Lazy<Action<Ring, int?>> lightIDSourceSetter = new(() =>
        typeof(Ring).GetCachedField("_lightSourceID", ReflectionCache.FlagTypes.InstanceFlags)
                    .GetInstanceFieldSetter<Ring, int?>());

    private static readonly Lazy<Func<Item, int>> _getDescriptionWidth = new(() => 
        typeof(Item).GetCachedMethod("getDescriptionWidth", ReflectionCache.FlagTypes.InstanceFlags)
                    .CreateDelegate<Func<Item, int>>());
    #endregion

    #region tooltips

    /// <summary>
    /// Called at warp, resets the tooltip map.
    /// </summary>
    internal static void Reset()
    {
        _tooltipMap.Value.Clear();
        _combinedTooltips.Clear();
        _bootsTooltips.Clear();
    }

    /// <summary>
    /// When returning to title screen, have to reset everything.
    /// </summary>
    internal static void OnReturnToTitle()
    {
        Reset();
        _activeEffects.ResetAllScreens();
        _tooltipMap.ResetAllScreens();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Item), nameof(Item.getExtraSpaceNeededForTooltipSpecialIcons))]
    private static void PostfixExtraSpaceItems(Item __instance, ref Point __result, int startingHeight, SpriteFont font, int horizontalBuffer, int minWidth)
    {
        try
        {
            if (__instance.TypeDefinitionId is "(H)" or "(S)" or "(P)" && GetEffectsForTooltip(__instance) is { } effects)
            {
                Point temp = AdjustExtraRows(__result, font, horizontalBuffer, effects);
                if (__result.Y == 0)
                {
                    __result.Y += startingHeight;
                }
                __result.Y += temp.Y;
                __result.X = Math.Max(Math.Max(__result.X, temp.X), minWidth);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding extra rows to items tooltip", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Ring), nameof(Ring.getExtraSpaceNeededForTooltipSpecialIcons))]
    private static void PostfixExtraSpaceRings(Ring __instance, ref Point __result, SpriteFont font, int horizontalBuffer)
    {
        try
        {
            if (GetEffectsForTooltip(__instance) is { } effects)
            {
                Point temp = AdjustExtraRows(__result, font, horizontalBuffer, effects);
                __result.Y += temp.Y;
                __result.X = Math.Max(__result.X, temp.X);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding extra rows to ring tooltip", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Boots), nameof(Boots.getExtraSpaceNeededForTooltipSpecialIcons))]
    private static void PostfixExtraSpaceBoots(Boots __instance, ref Point __result, SpriteFont font, int horizontalBuffer)
    {
        try
        {
            if (GetEffectsForTooltip(__instance) is { } effects)
            {
                Point temp = AdjustExtraRows(__result, font, horizontalBuffer, effects);
                __result.Y += temp.Y;
                __result.Y -= __instance.getNumberOfDescriptionCategories() * 48; // remove vanilla extra space.
                __result.X = Math.Max(__result.X, temp.X);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding extra rows to boots tooltip", ex);
        }
    }

    private static Point AdjustExtraRows(Point __result, SpriteFont font, int horizontalBuffer, EquipEffects effects)
    {
        int extra_rows = 0;
        BuffModel baseEffects = effects.BaseEffects;
        if (!string.IsNullOrWhiteSpace(effects.Condition))
        {
            extra_rows++;
        }

        if (effects.Light.Radius > 0)
        {
            extra_rows++;
        }

        if (effects.HealthRegen > 0)
        {
            extra_rows++;
        }

        if (effects.StaminaRegen > 0)
        {
            extra_rows++;
        }

        extra_rows += baseEffects.GetExtraRows();

        if (baseEffects.WeaponSpeedMultiplier != 0)
        {
            __result.X = Math.Max(
                __result.X,
                (int)font.MeasureString(I18n.WeaponSpeed(baseEffects.WeaponSpeedMultiplier.FormatPercent())).X + horizontalBuffer + 1);
        }
        if (baseEffects.WeaponPrecisionMultiplier != 0)
        {
            __result.X = Math.Max(
                __result.X,
                (int)font.MeasureString(I18n.WeaponPrecision(baseEffects.WeaponPrecisionMultiplier.FormatPercent())).X + horizontalBuffer + 1);
        }

        return new(__result.X, extra_rows * Math.Max((int)font.MeasureString("TT").Y, 48));
    }

    [HarmonyPostfix]
    [MethodImpl(TKConstants.Hot)]
    [HarmonyPatch(typeof(Ring), nameof(Ring.drawTooltip))]
    private static void PostfixRingdrawTooltip(Ring __instance, SpriteBatch spriteBatch, ref int x, ref int y, SpriteFont font, float alpha)
    {
        try
        {
            if (GetEffectsForTooltip(__instance) is { } effects)
            {
                y = DrawTooltipForEquipEffect(spriteBatch, x, y, font, alpha, effects);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"drawing ring tooltip for {__instance.QualifiedItemId}", ex);
        }
    }

    [HarmonyPostfix]
    [MethodImpl(TKConstants.Hot)]
    [HarmonyPatch(typeof(Item), nameof(Item.drawTooltip))]
    private static void PostfixItemdrawTooltip(Item __instance, SpriteBatch spriteBatch, ref int x, ref int y, SpriteFont font, float alpha)
    {
        try
        {
            if (__instance.TypeDefinitionId is "(H)" or "(S)" or "(P)" && GetEffectsForTooltip(__instance) is { } effects)
            {
                y = DrawTooltipForEquipEffect(spriteBatch, x, y, font, alpha, effects);
            }

        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"drawing item tooltip for {__instance.QualifiedItemId}", ex);
        }
    }

    [HarmonyPrefix]
    [MethodImpl(TKConstants.Hot)]
    [HarmonyPatch(typeof(Boots), nameof(Boots.drawTooltip))]
    private static bool PrefixDrawBootsToolTip(Boots __instance, SpriteBatch spriteBatch, ref int x, ref int y, SpriteFont font, float alpha)
    {
        try
        {
            if (GetEffectsForTooltip(__instance) is { } effects)
            {
                string description = Game1.parseText(__instance.description, Game1.smallFont, _getDescriptionWidth.Value(__instance));
                Utility.drawTextWithShadow(spriteBatch, description, font, new Vector2(x + 16, y + 16 + 4), Game1.textColor);
                y += (int)font.MeasureString(description).Y;
                y = DrawTooltipForEquipEffect(spriteBatch, x, y, font, alpha, effects);
                return false;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"drawing boots tooltip for {__instance.QualifiedItemId}", ex);
        }
        return true;
    }

    private static int DrawTooltipForEquipEffect(SpriteBatch spriteBatch, int x, int y, SpriteFont font, float alpha, EquipEffects effects)
    {
        int height = Math.Max((int)font.MeasureString("TT").Y, 48);
        if (!string.IsNullOrWhiteSpace(effects.Condition))
        {
            DrawText(I18n.CurrentEffects(), x, ref y);
        }
        if (effects.Light.Radius > 0)
        {
            DrawIcon(AssetManager.RingTextures, 0, x, y, 0);
            DrawText(I18n.EmitsLight(), x, ref y);
        }

        if (effects.StaminaRegen > 0)
        {
            DrawIcon(Game1.mouseCursors, 0, x, y);
            DrawText(I18n.RestoresStamina(effects.StaminaRegen.FormatNumber()), x, ref y);
        }

        if (effects.HealthRegen > 0)
        {
            DrawIcon(Game1.mouseCursors, 0, x, y, 438);
            DrawText(I18n.RestoresHealth(effects.HealthRegen.FormatNumber()), x, ref y);
        }

        BuffModel baseEffect = effects.BaseEffects;
        if (baseEffect.FarmingLevel != 0)
        {
            DrawIcon(Game1.mouseCursors, 1, x, y);
            DrawText(Game1.content.LoadString("Strings\\UI:ItemHover_Buff0", baseEffect.FarmingLevel.FormatNumber()), x, ref y);
        }
        if (baseEffect.FishingLevel != 0)
        {
            DrawIcon(Game1.mouseCursors, 2, x, y);
            DrawText(Game1.content.LoadString("Strings\\UI:ItemHover_Buff1", baseEffect.FishingLevel.FormatNumber()), x, ref y);
        }
        if (baseEffect.MiningLevel != 0)
        {
            DrawIcon(Game1.mouseCursors, 3, x, y);
            DrawText(Game1.content.LoadString("Strings\\UI:ItemHover_Buff2", baseEffect.MiningLevel.FormatNumber()), x, ref y);
        }
        if (baseEffect.CombatLevel != 0)
        {
            DrawIcon(Game1.mouseCursors, 14, x, y);
            DrawText(Game1.content.LoadString("Strings\\UI:ItemHover_Buff3", baseEffect.CombatLevel.FormatNumber()), x, ref y);
        }
        if (baseEffect.LuckLevel != 0)
        {
            DrawIcon(Game1.mouseCursors, 5, x, y);
            DrawText(Game1.content.LoadString("Strings\\UI:ItemHover_Buff4", baseEffect.LuckLevel.FormatNumber()), x, ref y);
        }
        if (baseEffect.ForagingLevel != 0)
        {
            DrawIcon(Game1.mouseCursors, 6, x, y);
            DrawText(Game1.content.LoadString("Strings\\UI:ItemHover_Buff5", baseEffect.ForagingLevel.FormatNumber()), x, ref y);
        }
        if (baseEffect.MaxStamina != 0)
        {
            DrawIcon(Game1.mouseCursors, 8, x, y);
            DrawText(Game1.content.LoadString("Strings\\UI:ItemHover_Buff6", baseEffect.MaxStamina.FormatNumber()), x, ref y);
        }
        if (baseEffect.MagneticRadius != 0)
        {
            DrawIcon(Game1.mouseCursors, 9, x, y);
            DrawText(Game1.content.LoadString("Strings\\UI:ItemHover_Buff8", baseEffect.MagneticRadius.FormatNumber()), x, ref y);
        }
        if (baseEffect.Speed != 0)
        {
            DrawIcon(Game1.mouseCursors, 10, x, y);
            DrawText(Game1.content.LoadString("Strings\\UI:ItemHover_Buff9", baseEffect.Speed.FormatNumber()), x, ref y);
        }
        if (baseEffect.Defense != 0)
        {
            DrawIcon(Game1.mouseCursors, 11, x, y);
            DrawText(I18n.Defense(baseEffect.Defense.FormatNumber()), x, ref y);
        }
        if (baseEffect.Attack != 0)
        {
            DrawIcon(Game1.mouseCursors, 12, x, y);
            DrawText(Game1.content.LoadString("Strings\\UI:ItemHover_Buff11", baseEffect.Attack.FormatNumber()), x, ref y);
        }
        if (baseEffect.AttackMultiplier != 0)
        {
            DrawIcon(Game1.mouseCursors, 12, x, y);
            DrawText(Game1.content.LoadString("Strings\\UI:ItemHover_Buff11", baseEffect.AttackMultiplier.FormatPercent()), x, ref y);
        }
        if (baseEffect.Immunity != 0)
        {
            DrawIcon(Game1.mouseCursors, 15, x, y);
            DrawText(I18n.Immunity(baseEffect.Immunity.FormatNumber()), x, ref y);
        }
        if (baseEffect.CriticalChanceMultiplier != 0)
        {
            DrawIcon(Game1.mouseCursors, 4, x, y);
            DrawText(I18n.Critchance(baseEffect.CriticalChanceMultiplier.FormatPercent()), x, ref y);
        }
        if (baseEffect.CriticalPowerMultiplier != 0)
        {
            DrawIcon(Game1.mouseCursors, 16, x, y);
            DrawText(I18n.Critpower(baseEffect.CriticalPowerMultiplier.FormatPercent()), x, ref y);
        }
        if (baseEffect.KnockbackMultiplier != 0)
        {
            DrawIcon(Game1.mouseCursors, 7, x, y);
            DrawText(I18n.Knockback(baseEffect.KnockbackMultiplier.FormatPercent()), x, ref y);
        }
        if (baseEffect.WeaponSpeedMultiplier != 0)
        {
            DrawIcon(Game1.mouseCursors, 13, x, y);
            DrawText(I18n.WeaponSpeed(baseEffect.WeaponSpeedMultiplier.FormatPercent()), x, ref y);
        }
        if (baseEffect.WeaponPrecisionMultiplier != 0)
        {
            DrawIcon(AssetManager.RingTextures, 1, x, y, 0);
            DrawText(I18n.WeaponPrecision(baseEffect.WeaponPrecisionMultiplier.FormatPercent()), x, ref y);
        }

        void DrawText(string text, int x, ref int y)
        {
            Utility.drawTextWithShadow(
                        spriteBatch,
                        text,
                        font,
                        new Vector2(x + 68, y + 28),
                        Game1.textColor * 0.9f * alpha);
            y += height;
        }

        void DrawIcon(Texture2D texture, int index, int x, int y, int offset = 428)
        {
            Utility.drawWithShadow(
                spriteBatch,
                texture,
                new Vector2(x + 20, y + 20),
                new Rectangle(index * 10, offset, 10, 10),
                Color.White,
                0f,
                Vector2.Zero,
                4f,
                flipped: false,
                1f);
        }

        return y;
    }

    private static string FormatNumber(this int number) => $"{number:+#;-#}";

    private static string FormatNumber(this float number) => $"{number:+0.0;-0.0}";

    private static string FormatPercent(this float number) => $"{number:+#.#%;-#.#%}";

    #endregion

    [HarmonyPostfix]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPatch(typeof(Ring), nameof(Ring.CanCombine))]
    private static void PostfixCanCombine(Ring __instance, Ring ring, ref bool __result)
    {
        if (!__result)
        {
            return;
        }
        try
        {
            if (AssetManager.GetEquipData(ring.QualifiedItemId)?.CanBeCombined == false || AssetManager.GetEquipData(__instance.QualifiedItemId)?.CanBeCombined == false)
            {
                __result = false;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("overriding combining rings", ex);
        }
    }

    #region monster slay

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Ring), nameof(Ring.onMonsterSlay))]
    private static void PostfixRingMonsterSlay(Ring __instance, GameLocation location, Farmer who)
    {
        try
        {
            AddMonsterKillBuff(__instance, location, who);
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding ring monster slay buff", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLocation), "onMonsterKilled")]
    private static void PostfixMonsterSlay(Farmer who, GameLocation __instance)
    {
        try
        {
            who.hat.Value?.AddMonsterKillBuff(__instance, who);
            who.shirtItem.Value?.AddMonsterKillBuff(__instance, who);
            who.pantsItem.Value?.AddMonsterKillBuff(__instance, who);
            who.boots.Value?.AddMonsterKillBuff(__instance, who);
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"adding monster slay buff", ex);
        }
    }

    private static void AddMonsterKillBuff(this Item item, GameLocation location, Farmer who)
    {
        EquipEffects? effect = AssetManager.GetEquipData(item.QualifiedItemId)
                        ?.GetEffect(EquipmentBuffTrigger.OnMonsterSlay, location, who);
        if (effect is not null)
        {
            effect.AddBuff(item, who);
            effect.AddRegen(who);
        }
    }

    #endregion

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Item), nameof(Item.AddEquipmentEffects))]
    private static void OnAddEffects(Item __instance, ref BuffEffects effects)
    {
        if (__instance is Tool)
        {
            return;
        }

        try
        {
            if (GetEquipEffect(__instance) is { } active)
            {
                _activeEffects.Value.AddOrUpdate(__instance, active);
                effects = active.BaseEffects.Merge(effects);

                ModEntry.ModMonitor.TraceOnlyLog($"[DataEquips] Added Effects for {__instance.QualifiedItemId}");
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"adding effects for {__instance.QualifiedItemId}", ex);
        }
    }

    #region equip/dequip

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Item), nameof(Item.onUnequip))]
    private static void OnUnequip(Item __instance, Farmer who)
    {
        // Ring.OnUnequip doesn't actually call base, but just in case.
        // banning tools.
        if (__instance.TypeDefinitionId is "(H)" or "(S)" or "(P)" or "(B)")
        {
            try
            {
                if (_activeEffects.Value.TryGetValue(__instance, out EquipEffects? effects))
                {
                    if (effects.Light.Radius > 0)
                    {
                        RemoveItemLight(__instance, who.currentLocation);
                    }
                    _activeEffects.Value.Remove(__instance);
                    ModEntry.ModMonitor.TraceOnlyLog($"[DataEquips] Unequip for {__instance.QualifiedItemId}");
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError($"unequipping {__instance.QualifiedItemId}", ex);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Ring), nameof(Ring.onUnequip))]
    private static void OnUnequipRing(Ring __instance, Farmer who)
    {
        try
        {
            if (_activeEffects.Value.TryGetValue(__instance, out EquipEffects? effects))
            {
                if (effects.Light.Radius > 0)
                {
                    RemoveRingLight(__instance, who.currentLocation);
                }
                _activeEffects.Value.Remove(__instance);
                ModEntry.ModMonitor.TraceOnlyLog($"[DataEquips] Unequip for {__instance.QualifiedItemId}");
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"unequipping {__instance.QualifiedItemId}", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Item), nameof(Item.onEquip))]
    private static void OnEquip(Item __instance, Farmer who)
    {
        // Rings need to be handled separately since I need to be at the end of Ring.onEquip, and Ring.onEquip calls base at the START.
        // banning tools.
        if (__instance.TypeDefinitionId is "(H)" or "(S)" or "(P)" or "(B)")
        {
            try
            {
                if (GetEquipEffect(__instance) is { } effect)
                {
                    LightData lightEffect = effect.Light;
                    if (lightEffect.Radius > 0)
                    {
                        _ = AddItemLight(lightEffect.Radius, lightEffect.Color, __instance, who, who.currentLocation);
                    }
                    _activeEffects.Value.AddOrUpdate(__instance, effect);
                    ModEntry.ModMonitor.TraceOnlyLog($"[DataEquips] Equip for {__instance.QualifiedItemId}");
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError($"equipping {__instance.QualifiedItemId}", ex);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Ring), nameof(Ring.onEquip))]
    private static void OnEquipRing(Ring __instance, Farmer who)
    {
        try
        {
            if (GetEquipEffect(__instance) is { } effect)
            {
                LightData lightEffect = effect.Light;
                if (lightEffect.Radius > 0)
                {
                    AddRingLight(lightEffect.Radius, lightEffect.Color, __instance, who, who.currentLocation);
                }
                _activeEffects.Value.AddOrUpdate(__instance, effect);
                ModEntry.ModMonitor.TraceOnlyLog($"[DataEquips] Equip for {__instance.QualifiedItemId}");
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"equipping {__instance.QualifiedItemId}", ex);
        }
    }

    #endregion

    /// <summary>
    /// Updates the current lights.
    /// </summary>
    /// <param name="e">Update ticked event args.</param>
    [MethodImpl(TKConstants.Hot)]
    internal static void UpdateEquips(UpdateTickedEventArgs e)
    {
        Farmer currentPlayer = Game1.player;
        if (!Context.IsWorldReady || currentPlayer.currentLocation is not GameLocation currentLocation)
        {
            return;
        }

        if (currentLocation.farmers.Count > 1)
        {
            bool capRadius = !currentLocation.IsOutdoors && currentLocation is not MineShaft && currentLocation is not VolcanoDungeon;
            foreach (Farmer? player in currentLocation.farmers)
            {
                if (ReferenceEquals(player, currentPlayer))
                {
                    continue;
                }

                Vector2 offset = player.shouldShadowBeOffset ? player.drawOffset : Vector2.Zero;
                offset.Y += 21f;
                offset += player.Position;

                player.hat.Value?.AdjustLight(currentLocation, offset, capRadius);
                player.shirtItem.Value?.AdjustLight(currentLocation, offset, capRadius);
                player.pantsItem.Value?.AdjustLight(currentLocation, offset, capRadius);
                player.boots.Value?.AdjustLight(currentLocation, offset, capRadius);
            }
        }

        if (_lightSources.Value.Count != 0)
        {
            Vector2 offset = currentPlayer.shouldShadowBeOffset ? currentPlayer.drawOffset : Vector2.Zero;
            offset.Y += 21f;
            offset += currentPlayer.Position;

            foreach (int light in _lightSources.Value.Values)
            {
                currentPlayer.currentLocation.repositionLightSource(light, offset);
            }
        }

        if (e.IsOneSecond && Game1.shouldTimePass())
        {
            float health_regen = 0;
            float stamina_regen = 0;

            foreach (KeyValuePair<Item, EquipEffects> kvp in _activeEffects.Value)
            {
                EquipEffects effect = kvp.Value;
                health_regen += effect.HealthRegen;
                stamina_regen += effect.StaminaRegen;
            }

            ModEntry.ModMonitor.TraceOnlyLog($"Health regen {health_regen}, stamina regen {stamina_regen}.");

            EquipEffects.HandleRegen(currentPlayer, health_regen, stamina_regen);
        }
    }

    private static void AdjustLight(this Item item, GameLocation location, Vector2 position, bool capRadius)
    {
        if (item.modData.GetInt(LightKey) is int lightId)
        {
            location.repositionLightSource(lightId, position);
            if (capRadius && location.getLightSource(lightId) is { } light)
            {
                light.radius.Value = 3f;
            }
        }
    }

    /// <summary>
    /// Handles moving between locations for lights.
    /// </summary>
    /// <param name="e">The on warp event args.</param>
    internal static void OnPlayerLocationChange(WarpedEventArgs e)
    {
        if (e.OldLocation is GameLocation old)
        {
            e.Player?.PlayerLeaveLocation(old);
        }

        if (e.NewLocation is GameLocation newLocation)
        {
            e.Player?.PlayerEnterLocation(newLocation);
        }
    }

    /// <summary>
    /// Called at day start, fakes an entry and exit for items.
    /// </summary>
    internal static void OnDayStart()
    {
        if (Game1.currentLocation is GameLocation current && Game1.player is Farmer player)
        {
            player.PlayerLeaveLocation(current);
            player.leftRing.Value?.onLeaveLocation(player, current);
            player.rightRing.Value?.onLeaveLocation(player, current);

            player.PlayerEnterLocation(current);
            player.leftRing.Value?.onNewLocation(player, current);
            player.rightRing.Value?.onNewLocation(player, current);

            player.buffs.Dirty = true;
        }
    }

    private static void PlayerEnterLocation(this Farmer farmer, GameLocation newLocation)
    {
        farmer.hat.Value?.ItemEnterLocation(farmer, newLocation);
        farmer.shirtItem.Value?.ItemEnterLocation(farmer, newLocation);
        farmer.pantsItem.Value?.ItemEnterLocation(farmer, newLocation);
        farmer.boots.Value?.ItemEnterLocation(farmer, newLocation);
    }

    private static void PlayerLeaveLocation(this Farmer farmer, GameLocation old)
    {
        farmer.hat.Value?.ItemLeaveLocation(old);
        farmer.shirtItem.Value?.ItemLeaveLocation(old);
        farmer.pantsItem.Value?.ItemLeaveLocation(old);
        farmer.boots.Value?.ItemLeaveLocation(old);
    }

    private static void ItemEnterLocation(this Item item, Farmer who, GameLocation location)
    {
        EquipEffects? newEffect = NewLocation(item, who, location);
        if (newEffect is not null && newEffect.Light.Radius > 0)
        {
            int radius = !location.IsOutdoors && location is not MineShaft or VolcanoDungeon ? 3 : newEffect.Light.Radius;
            AddItemLight(radius, newEffect.Light.Color, item, who, location);
        }
    }

    private static void ItemLeaveLocation(this Item item, GameLocation location)
    {
        if (GetEquipEffect(item)?.Light?.Radius > 0)
        {
            RemoveItemLight(item, location);
            ModEntry.ModMonitor.TraceOnlyLog($"[DataEquips] LeaveLocation for {item.QualifiedItemId}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Ring), nameof(Ring.onNewLocation))]
    private static void OnNewLocation(Ring __instance, Farmer who, GameLocation environment)
    {
        try
        {
            EquipEffects? newEffect = NewLocation(__instance, who, environment);
            if (newEffect is not null && newEffect.Light.Radius > 0)
            {
                AddRingLight(newEffect.Light.Radius, newEffect.Light.Color, __instance, who, environment);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"new location for {__instance.QualifiedItemId}", ex);
        }
    }

    private static EquipEffects? NewLocation(Item item, Farmer who, GameLocation environment)
    {
        if (AssetManager.GetEquipData(item.QualifiedItemId) is { } data)
        {
            EquipEffects? newEffect = data.GetEffect(EquipmentBuffTrigger.OnEquip, environment, who);
            _activeEffects.Value.TryGetValue(item, out EquipEffects? ringEffects);

            if (!ReferenceEquals(ringEffects, newEffect))
            {
                who.buffs.Dirty = true;
                if (newEffect is not null)
                {
                    _activeEffects.Value.AddOrUpdate(item, newEffect);
                    _tooltipMap.Value[item.QualifiedItemId] = newEffect;
                }
                else
                {
                    _activeEffects.Value.Remove(item);
                    _tooltipMap.Value.Remove(item.QualifiedItemId);
                }
            }

            ModEntry.ModMonitor.TraceOnlyLog($"[DataEquips] NewLocation for {item.QualifiedItemId}");
            return newEffect;
        }
        return null;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Ring), nameof(Ring.onLeaveLocation))]
    private static void OnLeaveLocation(Ring __instance, GameLocation environment)
    {
        try
        {
            if (GetEquipEffect(__instance)?.Light?.Radius > 0)
            {
                RemoveRingLight(__instance, environment);
                ModEntry.ModMonitor.TraceOnlyLog($"[DataEquips] LeaveLocation for {__instance.QualifiedItemId}");
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"leave location for {__instance.QualifiedItemId}", ex);
        }
    }

    private static EquipEffects? GetEquipEffect(Item __instance)
    {
        if (_activeEffects.Value.TryGetValue(__instance, out EquipEffects? effects))
        {
            _tooltipMap.Value[__instance.QualifiedItemId] = effects;
            return effects;
        }

        if (!_tooltipMap.Value.TryGetValue(__instance.QualifiedItemId, out EquipEffects? ringEffects))
        {
            ringEffects = AssetManager.GetEquipData(__instance.QualifiedItemId)?.GetEffect(EquipmentBuffTrigger.OnEquip);
            if (ringEffects is not null)
            {
                _tooltipMap.Value[__instance.QualifiedItemId] = ringEffects;
            }
        }

        return ringEffects;
    }

    private static IEnumerable<EquipEffects> GetAllRingEffects(Ring __instance)
    {
        if (__instance is CombinedRing combined)
        {
            foreach (Ring? ring in combined.combinedRings)
            {
                foreach (EquipEffects effect in GetAllRingEffects(ring))
                {
                    yield return effect;
                }
            }
        }
        else if (GetEquipEffect(__instance) is { } ringEffect)
        {
            yield return ringEffect;
        }
    }

    private static EquipEffects? GetEffectsForTooltip(Boots boots)
    {
        if (_bootsTooltips.TryGetValue(boots, out EquipEffects? val))
        {
            return val;
        }
        else if (GetEquipEffect(boots) is { } bootsEffect)
        {
            EquipEffects copy = new();
            if (bootsEffect.Light.Radius > 0)
            {
                copy.Light.Radius = bootsEffect.Light.Radius;
            }
            if (bootsEffect.HealthRegen > 0)
            {
                copy.HealthRegen += bootsEffect.HealthRegen;
            }
            if (bootsEffect.StaminaRegen > 0)
            {
                copy.StaminaRegen += bootsEffect.StaminaRegen;
            }
            if (!string.IsNullOrWhiteSpace(bootsEffect.Condition))
            {
                copy.Condition = bootsEffect.Condition;
            }
            BuffModel.LeftFold(copy.BaseEffects, bootsEffect.BaseEffects);
            copy.BaseEffects.Defense += boots.defenseBonus.Value;
            copy.BaseEffects.Immunity += boots.immunityBonus.Value;
            _bootsTooltips.AddOrUpdate(boots, copy);
            return copy;
        }
        else
        {
            _bootsTooltips.AddOrUpdate(boots, null);
            return null;
        }
    }

    private static EquipEffects? GetEffectsForTooltip(Item instance)
    {
        if (instance is CombinedRing combined)
        {
            if (_combinedTooltips.TryGetValue(combined, out EquipEffects? val))
            {
                return val;
            }
            else if (GetAllRingEffects(combined).Any())
            {
                EquipEffects combinedEffects = new();
                foreach (EquipEffects e in GetAllRingEffects(combined))
                {
                    if (e.Light.Radius > 0)
                    {
                        combinedEffects.Light.Radius += e.Light.Radius;
                    }
                    if (e.HealthRegen > 0)
                    {
                        combinedEffects.HealthRegen += e.HealthRegen;
                    }
                    if (e.StaminaRegen > 0)
                    {
                        combinedEffects.StaminaRegen += e.StaminaRegen;
                    }
                    if (!string.IsNullOrWhiteSpace(e.Condition))
                    {
                        combinedEffects.Condition = e.Condition;
                    }
                    BuffModel.LeftFold(combinedEffects.BaseEffects, e.BaseEffects);
                }
                _combinedTooltips.AddOrUpdate(combined, combinedEffects);
                return combinedEffects;
            }
            else
            {
                _combinedTooltips.AddOrUpdate(combined, null);
                return null;
            }
        }
        else if (instance is Boots boots)
        {
            return GetEffectsForTooltip(boots);
        }
        else
        {
            return GetEquipEffect(instance);
        }
    }

    private static int AddItemLight(int radius, Color color, Item item, Farmer player, GameLocation location)
    {
        // rings have their own unique item ID, but other items don't. We're gonna cheat a little and use the hash code, which in C# is the sync block index unless defined otherwise.
        // should be unique enough.
        int lightID = GenerateLightSource(radius, color, player, location, item.GetHashCode());
        item.modData.SetInt(LightKey, lightID);
        _lightSources.Value[item] = lightID;
        ModEntry.ModMonitor.TraceOnlyLog($"[DataEquips] Adding light id {lightID:X}");
        return lightID;
    }

    private static void AddRingLight(int radius, Color color, Ring ring, Farmer player, GameLocation location)
    {
        int lightID = GenerateLightSource(radius, color, player, location, ring.uniqueID.Value);
        lightIDSourceSetter.Value(ring, lightID);
        ModEntry.ModMonitor.TraceOnlyLog($"[DataEquips] Adding light id {lightID:X}");
    }

    private static int GenerateLightSource(int radius, Color color, Farmer player, GameLocation location, int uniqueItemID)
    {
        int startingID;
        int lightID;

        unchecked
        {
            lightID = startingID = uniqueItemID + (int)player.UniqueMultiplayerID;
            while (location.sharedLights.ContainsKey(lightID))
            {
                ++lightID;
            }
        }

        LightSource lightSource = new(
                    textureIndex: 1,
                    new Vector2(player.Position.X + 21f, player.Position.Y + 64f),
                    radius,
                    color,
                    identifier: startingID,
                    light_context: LightSource.LightContext.None,
                    playerID: player.UniqueMultiplayerID);
        location.sharedLights[lightID] = lightSource;
        return lightID;
    }

    private static void RemoveRingLight(Ring __instance, GameLocation location)
    {
        int? lightID = lightIDSourceGetter.Value(__instance);
        if (lightID.HasValue)
        {
            ModEntry.ModMonitor.TraceOnlyLog($"[DataEquips] Removing light id {lightID.Value:X}");
            location.removeLightSource(lightID.Value);
            lightIDSourceSetter.Value(__instance, null);
        }
    }

    private static void RemoveItemLight(Item item, GameLocation location)
    {
        if (_lightSources.Value.TryGetValue(item, out int lightID))
        {
            ModEntry.ModMonitor.TraceOnlyLog($"[DataEquips] Removing light id {lightID:X}");
            location.removeLightSource(lightID);
            item.modData.Remove(LightKey);
            _lightSources.Value.Remove(item);
        }
    }
}