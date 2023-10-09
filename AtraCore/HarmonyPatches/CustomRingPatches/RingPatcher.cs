namespace AtraCore.HarmonyPatches.CustomRingPatches;

using System.Runtime.CompilerServices;

using AtraBase.Toolkit;
using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.Caches.AssetCache;
using AtraCore.Framework.Models;
using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.Buffs;
using StardewValley.Objects;

/// <summary>
/// Holds patches against the <see cref="Ring"/> class for custom rings.
/// </summary>
[HarmonyPatch(typeof(Ring))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class RingPatcher
{
    // maps the ring ID to the current effect of the ring for tooltips
    private static readonly Dictionary<string, RingEffects> _tooltipMap = new();

    // maps the rings to their active effects
    private static readonly ConditionalWeakTable<Ring, RingEffects> _activeEffects = new();

    // holds tooltip cache for combined rings
    private static readonly ConditionalWeakTable<CombinedRing, RingEffects?> _combinedTooltips = new();

    #region delegates
    private static readonly Lazy<Func<Ring, int?>> lightIDSourceGetter = new(() =>
        typeof(Ring).GetCachedField("_lightSourceID", ReflectionCache.FlagTypes.InstanceFlags)
                    .GetInstanceFieldGetter<Ring, int?>());

    private static readonly Lazy<Action<Ring, int?>> lightIDSourceSetter = new(() =>
        typeof(Ring).GetCachedField("_lightSourceID", ReflectionCache.FlagTypes.InstanceFlags)
                    .GetInstanceFieldSetter<Ring, int?>());
    #endregion

    #region tooltips

    /// <summary>
    /// Called at warp, resets the tooltip map.
    /// </summary>
    internal static void Reset()
    {
        _tooltipMap.Clear();
        _combinedTooltips.Clear();
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Ring.getExtraSpaceNeededForTooltipSpecialIcons))]
    private static void PostfixExtraSpace(Ring __instance, ref Point __result, SpriteFont font, int horizontalBuffer)
    {
        try
        {
            if (GetEffectsForTooltip(__instance) is { } effects)
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

                extra_rows += baseEffects.GetExtraRows();

                __result.Y += extra_rows * Math.Max((int)font.MeasureString("TT").Y, 48);

                if (baseEffects.WeaponSpeedMultiplier != 0)
                {
                    __result.X = Math.Max(__result.X, (int)font.MeasureString(I18n.WeaponSpeed(baseEffects.WeaponSpeedMultiplier.FormatPercent())).X + horizontalBuffer + 1);
                }
                if (baseEffects.WeaponPrecisionMultiplier != 0)
                {
                    __result.X = Math.Max(__result.X, (int)font.MeasureString(I18n.WeaponPrecision(baseEffects.WeaponPrecisionMultiplier.FormatPercent())).X + horizontalBuffer + 1);
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding extra rows to ring tooltip", ex);
        }
    }

    [HarmonyPostfix]
    [MethodImpl(TKConstants.Hot)]
    [HarmonyPatch(nameof(Ring.drawTooltip))]
    private static void PostfixdrawTooltip(Ring __instance, SpriteBatch spriteBatch, ref int x, ref int y, SpriteFont font, float alpha)
    {
        try
        {
            if (GetEffectsForTooltip(__instance) is { } effects)
            {
                int height = Math.Max((int)font.MeasureString("TT").Y, 48);
                if (!string.IsNullOrWhiteSpace(effects.Condition))
                {
                    DrawText(I18n.CurrentEffects(), x, ref y);
                }
                if (effects.Light.Radius > 0)
                {
                    DrawIcon(AssetManager.RingTextures, 0, x, y, false);
                    DrawText(I18n.EmitsLight(), x, ref y);
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
                    DrawIcon(AssetManager.RingTextures, 1, x, y, false);
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

                void DrawIcon(Texture2D texture, int index, int x, int y, bool offset = true)
                {
                    Utility.drawWithShadow(
                        spriteBatch,
                        texture,
                        new Vector2(x + 20, y + 20),
                        new Rectangle(index * 10, offset ? 428 : 0, 10, 10),
                        Color.White,
                        0f,
                        Vector2.Zero,
                        4f,
                        flipped: false,
                        1f);
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"drawing ring tooltip for {__instance.QualifiedItemId}", ex);
        }
    }

    private static string FormatNumber(this int number) => (number > 0 ? "+" : string.Empty) + number.ToString();

    private static string FormatPercent(this float number) => (number > 0 ? "+" : string.Empty) + number.ToString("P0");

    #endregion

    [HarmonyPostfix]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPatch(nameof(Ring.CanCombine))]
    private static void PostfixCanCombine(Ring __instance, Ring ring, ref bool __result)
    {
        if (!__result)
        {
            return;
        }
        try
        {
            if (AssetManager.GetRingData(ring.ItemId)?.CanBeCombined == false || AssetManager.GetRingData(__instance.ItemId)?.CanBeCombined == false)
            {
                __result = false;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("overriding combining rings", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Ring.onMonsterSlay))]
    private static void PostfixMonsterSlay(Ring __instance, GameLocation location, Farmer who)
    {
        try
        {
            AssetManager.GetRingData(__instance.ItemId)
                ?.GetEffect(RingBuffTrigger.OnMonsterSlay, location, who)
                ?.AddBuff(__instance, who);
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding monster slay buff", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Ring.AddEquipmentEffects))]
    private static void OnAddEffects(Ring __instance, BuffEffects effects)
    {
        try
        {
            if (GetRingEffect(__instance) is { } ringEffects)
            {
                _activeEffects.AddOrUpdate(__instance, ringEffects);
                ringEffects.BaseEffects.Merge(effects);

                ModEntry.ModMonitor.TraceOnlyLog($"Added Effects for {__instance.QualifiedItemId}");
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"adding effects for {__instance.QualifiedItemId}", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Ring.onUnequip))]
    private static void OnUnequip(Ring __instance, Farmer who)
    {
        try
        {
            if (_activeEffects.TryGetValue(__instance, out RingEffects? effects))
            {
                if (effects.Light.Radius > 0)
                {
                    RemoveLightFrom(__instance, who.currentLocation);
                }
                _activeEffects.Remove(__instance);
                ModEntry.ModMonitor.TraceOnlyLog($"Unequip for {__instance.QualifiedItemId}");
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"unequipping {__instance.QualifiedItemId}", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Ring.onEquip))]
    private static void OnEquip(Ring __instance, Farmer who)
    {
        try
        {
            if (GetRingEffect(__instance) is { } effect)
            {
                LightData lightEffect = effect.Light;
                if (lightEffect.Radius > 0)
                {
                    AddLight(lightEffect.Radius, lightEffect.Color, __instance, who, who.currentLocation);
                }
                _activeEffects.AddOrUpdate(__instance, effect);
                ModEntry.ModMonitor.TraceOnlyLog($"Equip for {__instance.QualifiedItemId}");
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"equipping {__instance.QualifiedItemId}", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Ring.onNewLocation))]
    private static void OnNewLocation(Ring __instance, Farmer who, GameLocation environment)
    {
        try
        {
            if (AssetManager.GetRingData(__instance.ItemId) is { } data)
            {
                RingEffects? newEffect = data.GetEffect(RingBuffTrigger.OnEquip, environment, who);
                _activeEffects.TryGetValue(__instance, out RingEffects? ringEffects);

                if (!ReferenceEquals(ringEffects, newEffect))
                {
                    who.buffs.Dirty = true;
                    UpdateCache(__instance, newEffect);
                }

                if (newEffect is not null && newEffect.Light.Radius > 0)
                {
                    AddLight(newEffect.Light.Radius, newEffect.Light.Color, __instance, who, environment);
                }
                ModEntry.ModMonitor.TraceOnlyLog($"NewLocation for {__instance.QualifiedItemId}");
            }
            static void UpdateCache(Ring __instance, RingEffects? newEffect)
            {
                if (newEffect is not null)
                {
                    _activeEffects.AddOrUpdate(__instance, newEffect);
                    _tooltipMap[__instance.ItemId] = newEffect;
                }
                else
                {
                    _activeEffects.Remove(__instance);
                    _tooltipMap.Remove(__instance.ItemId);
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"new location for {__instance.QualifiedItemId}", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Ring.onLeaveLocation))]
    private static void OnLeaveLocation(Ring __instance, GameLocation environment)
    {
        try
        {
            if (GetRingEffect(__instance)?.Light?.Radius > 0)
            {
                RemoveLightFrom(__instance, environment);
                ModEntry.ModMonitor.TraceOnlyLog($"LeaveLocation for {__instance.QualifiedItemId}");
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"leave location for {__instance.QualifiedItemId}", ex);
        }
    }

    private static RingEffects? GetRingEffect(Ring __instance)
    {
        if (_activeEffects.TryGetValue(__instance, out RingEffects? effects))
        {
            _tooltipMap[__instance.ItemId] = effects;
            return effects;
        }

        if (!_tooltipMap.TryGetValue(__instance.ItemId, out RingEffects? ringEffects))
        {
            ringEffects = AssetManager.GetRingData(__instance.ItemId)?.GetEffect(RingBuffTrigger.OnEquip);
            if (ringEffects is not null)
            {
                _tooltipMap[__instance.ItemId] = ringEffects;
            }
        }

        return ringEffects;
    }

    private static IEnumerable<RingEffects> GetAllRingEffects(Ring __instance)
    {
        if (__instance is CombinedRing combined)
        {
            foreach (Ring? ring in combined.combinedRings)
            {
                foreach (RingEffects effect in GetAllRingEffects(ring))
                {
                    yield return effect;
                }
            }
        }
        else if (GetRingEffect(__instance) is { } ringEffect)
        {
            yield return ringEffect;
        }
    }

    private static RingEffects? GetEffectsForTooltip(Ring instance)
    {
        if (instance is CombinedRing combined)
        {
            if (_combinedTooltips.TryGetValue(combined, out RingEffects? val))
            {
                return val;
            }
            else if (GetAllRingEffects(combined).Any())
            {
                RingEffects combinedEffects = new();
                foreach (RingEffects e in GetAllRingEffects(combined))
                {
                    if (e.Light.Radius > 0)
                    {
                        combinedEffects.Light.Radius = e.Light.Radius;
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
        else
        {
            return GetRingEffect(instance);
        }
    }

    private static void AddLight(int radius, Color color, Ring instance, Farmer player, GameLocation location)
    {
        int startingID;
        int lightID;

        unchecked
        {
            startingID = instance.uniqueID.Value + (int)player.UniqueMultiplayerID;
            lightID = startingID;
            while (location.sharedLights.ContainsKey(lightID))
            {
                ++lightID;
            }
        }

        lightIDSourceSetter.Value(instance, lightID);
        ModEntry.ModMonitor.TraceOnlyLog($"[DataRings] Adding light id {lightID}");
        location.sharedLights[lightID] = new LightSource(
            textureIndex: 1,
            new Vector2(player.Position.X + 21f, player.Position.Y + 64f),
            radius,
            color,
            identifier: startingID,
            light_context: LightSource.LightContext.None,
            playerID: player.UniqueMultiplayerID);
    }

    private static void RemoveLightFrom(Ring __instance, GameLocation location)
    {
        int? lightID = lightIDSourceGetter.Value(__instance);
        if (lightID.HasValue)
        {
            ModEntry.ModMonitor.TraceOnlyLog($"[DataRings] Removing light id {lightID.Value}");
            location.removeLightSource(lightID.Value);
            lightIDSourceSetter.Value(__instance, null);
        }
    }
}
