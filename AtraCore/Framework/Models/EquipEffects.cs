// Ignore Spelling: Knockback

namespace AtraCore.Framework.Models;

using System.Reflection;

using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.Caches.AssetCache;
using AtraCore.Framework.ReflectionManager;
using AtraCore.HarmonyPatches.CustomEquipPatches;

using AtraShared.Utils.Extensions;

using FastExpressionCompiler.LightExpression;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Netcode;

using StardewValley.Buffs;
using StardewValley.GameData.Buffs;
using StardewValley.TokenizableStrings;

/// <summary>
/// The possible times to trigger a buff from the equipment.
/// </summary>
[Flags]
public enum EquipmentBuffTrigger
{
    /// <summary>
    /// Buffs are added when the ring is equipped.
    /// </summary>
    OnEquip = 0b1,

    /// <summary>
    /// Buffs are added when a monster is slain when the ring is equipped.
    /// </summary>
    OnMonsterSlay = 0b10,

    /// <summary>
    /// Buffs are added when the player is hit.
    /// </summary>
    OnPlayerHit = 0b100,
}

/// <summary>
/// A data model representing valid ring effects.
/// </summary>
public sealed class EquipmentExtModel
{
    private static readonly HashSet<string> NonDeterministicQueries = new(StringComparer.OrdinalIgnoreCase)
    {
        "RANDOM",
        "SYNCED_CHOICE",
        "SYNCED_RANDOM",
        "SYNCED_SUMMER_RAIN_RANDOM",
    };

    /// <summary>
    /// Gets or sets a list of the effects for this ring. Effects are checked in order.
    /// </summary>
    public List<EquipEffects> Effects { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether this ring should be eligible for combining. 
    /// </summary>
    public bool CanBeCombined { get; set; } = true;

    /// <summary>
    /// Gets the current valid effect for this ring.
    /// </summary>
    /// <param name="trigger">Which type of trigger to use.</param>
    /// <param name="location">The location to use, or null for current location.</param>
    /// <param name="player">The player to use, or null for current player.</param>
    /// <returns>The ring effects.</returns>
    internal EquipEffects? GetEffect(EquipmentBuffTrigger trigger, GameLocation? location = null, Farmer? player = null)
    {
        location ??= Game1.currentLocation;
        player ??= Game1.player;

        if (location is null || player is null)
        {
            return null;
        }

        foreach (EquipEffects effect in this.Effects)
        {
            if (effect.Trigger.HasFlag(trigger)
                && GameStateQuery.CheckConditions(effect.Condition, location, player, random: Random.Shared, ignoreQueryKeys: trigger == EquipmentBuffTrigger.OnEquip ? NonDeterministicQueries : null))
            {
                return effect;
            }
        }

        return null;
    }
}

/// <summary>
/// The effects possible on a ring.
/// </summary>
public sealed class EquipEffects
{
    /// <summary>
    /// Gets or sets a Id to facilitate CP editing. This should be unique within the list.
    /// </summary>
    public string Id { get; set; } = "Default";

    /// <summary>
    /// Gets or sets the ID of the buff to use, or null to use the default value.
    /// </summary>
    public string? BuffID { get; set; } = null;

    /// <summary>
    /// Gets or sets a condition to check, or null for no conditions.
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// Gets or sets how to display buffs for <see cref="EquipmentBuffTrigger.OnMonsterSlay"/>.
    /// </summary>
    public BuffDisplayAttributes DisplayAttributes { get; set; } = new();

    /// <summary>
    /// Gets or sets the games' various buff effects.
    /// </summary>
    public BuffModel BaseEffects { get; set; } = new();

    /// <summary>
    /// Gets or sets the lighting effect provided by this ring.
    /// </summary>
    public LightData Light { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating how much health should be regenerated.
    /// </summary>
    public float HealthRegen { get; set; } = 0;

    /// <summary>
    /// Gets or sets a value indicating how much stamina should be regenerated.
    /// </summary>
    public float StaminaRegen { get; set; } = 0f;

    /// <summary>
    /// Gets or sets a value indicating when to apply the buffs from this ring.
    /// </summary>
    public EquipmentBuffTrigger Trigger { get; set; } = EquipmentBuffTrigger.OnEquip;

    /// <summary>
    /// Adds the buff defined by this item instance to the player.
    /// </summary>
    /// <param name="instance">The item instance.</param>
    /// <param name="who">The farmer in question.</param>
    internal void AddBuff(Item instance, Farmer who)
    {
        string id = this.BuffID ?? instance.QualifiedItemId;
        if (who.hasBuff(id))
        {
            return;
        }

        BuffDisplayAttributes attributes = this.DisplayAttributes;
        Texture2D? tex = null;
        if (attributes.Texture is string textureName)
        {
            tex = AssetCache.Get<Texture2D>(textureName)?.Value;
        }

        Buff buff = new(
            id: id,
            displayName: TokenParser.ParseText(attributes.DisplayName) ?? instance.DisplayName,
            description: TokenParser.ParseText(attributes.Description) ?? instance.getDescription(),
            displaySource: instance.DisplayName,
            iconTexture: tex,
            iconSheetIndex: attributes.SpriteIndex,
            duration: attributes.Duration,
            effects: this.BaseEffects.ToBuffEffect()
            );
        who.applyBuff(buff);
    }

    /// <summary>
    /// Adds in the health/stamina regen.
    /// </summary>
    /// <param name="farmer">The farmer to add to.</param>
    internal void AddRegen(Farmer farmer) => HandleRegen(farmer, this.HealthRegen, this.StaminaRegen);

    /// <summary>
    /// Handles the regeneration for a specific amount for a player.
    /// </summary>
    /// <param name="currentPlayer">The player to increment for.</param>
    /// <param name="health_regen">The amount of health.</param>
    /// <param name="stamina_regen">The amount of stamina.</param>
    internal static void HandleRegen(Farmer currentPlayer, float health_regen, float stamina_regen)
    {
        if (health_regen > 0 && currentPlayer.health < currentPlayer.maxHealth - 1)
        {
            float amount = Math.Min(health_regen + ItemPatcher.HealthRemainder, currentPlayer.maxHealth - currentPlayer.health);
            int toAdd = (int)amount;
            if (toAdd > 0)
            {
                currentPlayer.health += toAdd;
                if (ModEntry.Config.ShowRegenNumbers)
                {
                    currentPlayer.currentLocation.debris.Add(new Debris(toAdd, currentPlayer.Position, Color.Green, 1f, currentPlayer));
                }

                ItemPatcher.HealthRemainder = amount - toAdd;
            }
            else
            {
                ItemPatcher.HealthRemainder = amount;
            }
        }

        if (stamina_regen > 0 && currentPlayer.Stamina < currentPlayer.MaxStamina)
        {
            int prev = (int)currentPlayer.Stamina;
            float amount = Math.Min(stamina_regen, currentPlayer.MaxStamina - currentPlayer.Stamina);
            currentPlayer.Stamina += amount;

            if (ModEntry.Config.ShowRegenNumbers)
            {
                int incremented = (int)currentPlayer.Stamina - prev;
                if (incremented > 0)
                {
                    currentPlayer.currentLocation.debris.Add(new Debris(incremented, currentPlayer.Position, Color.Yellow, 1f, currentPlayer));
                }
            }
        }
    }
}

/// <summary>
/// A model that represents possible buffs to the player.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Preference")]
public sealed class BuffModel : BuffAttributesData
{
    #region delegates

    // merges the model data with the given buff effects.
    private static readonly Lazy<Action<BuffEffects, BuffModel>> _merger = new(() =>
    {
        ParameterExpression effects = Expression.ParameterOf<BuffEffects>("effects");
        ParameterExpression model = Expression.ParameterOf<BuffModel>("model");

        List<Expression> expressions = [];

        foreach (FieldInfo field in typeof(BuffModel).GetFields())
        {
            MemberInfo member = typeof(BuffEffects).GetMember(field.Name).SingleOrDefault() ?? ReflectionThrowHelper.ThrowMethodNotFoundException<MemberInfo>(field.Name);
            MemberExpression getter = Expression.Field(model, field);
            switch (member)
            {
                case FieldInfo asField:
                {
                    if (AsBaseType(asField.FieldType) is { } type)
                    {
                        MemberExpression netField = Expression.Field(effects, asField);
                        MemberExpression value = Expression.Property(netField, asField.FieldType.GetCachedProperty("Value", ReflectionCache.FlagTypes.InstanceFlags));
                        BinaryExpression assign = Expression.Assign(value, Expression.Add(value, Expression.Convert(getter, type)));
                        expressions.Add(assign);
                    }
                    break;
                }
                case PropertyInfo asProperty:
                {
                    if (AsBaseType(asProperty.PropertyType) is { } type)
                    {
                        MemberExpression netField = Expression.Property(effects, asProperty);
                        MemberExpression value = Expression.Property(netField, asProperty.PropertyType.GetCachedProperty("Value", ReflectionCache.FlagTypes.InstanceFlags));
                        BinaryExpression assign = Expression.Assign(value, Expression.Add(value, Expression.Convert(getter, type)));
                    }
                    break;
                }
                default:
                    ReflectionThrowHelper.ThrowMethodNotFoundException(field.Name);
                    break;
            }
        }

        foreach (PropertyInfo property in typeof(BuffModel).GetProperties())
        {
            MemberInfo member = typeof(BuffEffects).GetMember(property.Name).SingleOrDefault() ?? ReflectionThrowHelper.ThrowMethodNotFoundException<MemberInfo>(property.Name);
            MemberExpression getter = Expression.Property(model, property);

            switch (member)
            {
                case FieldInfo asField:
                {
                    if (AsBaseType(asField.FieldType) is { } type)
                    {
                        MemberExpression netField = Expression.Field(effects, asField);
                        MemberExpression value = Expression.Property(netField, asField.FieldType.GetCachedProperty("Value", ReflectionCache.FlagTypes.InstanceFlags));
                        BinaryExpression assign = Expression.Assign(value, Expression.Add(value, Expression.Convert(getter, type)));
                        expressions.Add(assign);
                    }
                    break;
                }
                case PropertyInfo asProperty:
                {
                    if (AsBaseType(asProperty.PropertyType) is { } type)
                    {
                        MemberExpression netField = Expression.Property(effects, asProperty);
                        MemberExpression value = Expression.Property(netField, asProperty.PropertyType.GetCachedProperty("Value", ReflectionCache.FlagTypes.InstanceFlags));
                        BinaryExpression assign = Expression.Assign(value, Expression.Add(value, Expression.Convert(getter, type)));
                    }
                    break;
                }
                default:
                    ReflectionThrowHelper.ThrowMethodNotFoundException(property.Name);
                    break;
            }
        }

        BlockExpression block = Expression.Block(expressions);
        Expression<Action<BuffEffects, BuffModel>> lambda = Expression.Lambda<Action<BuffEffects, BuffModel>>(block, new ParameterExpression[] { effects, model });
        ModEntry.ModMonitor.LogIfVerbose($"Ring merge function generated:\n{lambda.ToCSharpString()}");
        return lambda.CompileFast();
    });

    // counts the number of extra rows needed for the tooltip.
    private static readonly Lazy<Func<BuffModel, int>> _extraRows = new(() =>
    {
        List<Expression> expressions = [];

        ParameterExpression model = Expression.ParameterOf<BuffModel>("model");
        ParameterExpression rows = Expression.ParameterOf<int>("rows");
        BinaryExpression init = Expression.Assign(rows, Expression.ZeroConstant);
        expressions.Add(init);

        foreach (FieldInfo field in typeof(BuffModel).GetFields())
        {
            MemberExpression getter = Expression.Field(model, field);
            BinaryExpression notequal = Expression.NotEqual(getter, field.FieldType == typeof(float) ? Expression.ConstantOf(0f) : Expression.ZeroConstant);
            ConditionalExpression elif = Expression.IfThen(notequal, Expression.PreIncrementAssign(rows));
            expressions.Add(elif);
        }

        foreach (PropertyInfo property in typeof(BuffModel).GetProperties())
        {
            MemberExpression getter = Expression.Property(model, property);
            BinaryExpression notequal = Expression.NotEqual(getter, property.PropertyType == typeof(float) ? Expression.ConstantOf(0f) : Expression.ZeroConstant);
            ConditionalExpression elif = Expression.IfThen(notequal, Expression.PreIncrementAssign(rows));
            expressions.Add(elif);
        }

        expressions.Add(rows);
        BlockExpression block = Expression.Block(new ParameterExpression[] { rows }, expressions);
        Expression<Func<BuffModel, int>> lambda = Expression.Lambda<Func<BuffModel, int>>(block, new ParameterExpression[] { model });
        ModEntry.ModMonitor.LogIfVerbose($"Height function generated:\n{lambda.ToCSharpString()}");
        return lambda.CompileFast();
    });

    // merges two buffmodels together into the left one.
    private static readonly Lazy<Action<BuffModel, BuffModel>> _leftFold = new(() =>
    {
        List<Expression> expressions = [];

        ParameterExpression left = Expression.ParameterOf<BuffModel>("left");
        ParameterExpression right = Expression.ParameterOf<BuffModel>("right");

        foreach (FieldInfo field in typeof(BuffModel).GetFields())
        {
            MemberExpression getter = Expression.Field(right, field);
            MemberExpression setter = Expression.Field(left, field);
            BinaryExpression sum = Expression.Assign(setter, Expression.Add(setter, getter));
            expressions.Add(sum);
        }

        foreach (PropertyInfo property in typeof(BuffModel).GetProperties())
        {
            MemberExpression getter = Expression.Property(right, property);
            MemberExpression setter = Expression.Property(left, property);
            BinaryExpression sum = Expression.Assign(setter, Expression.Add(setter, getter));
            expressions.Add(sum);
        }

        BlockExpression block = Expression.Block(expressions);
        Expression<Action<BuffModel, BuffModel>> lambda = Expression.Lambda<Action<BuffModel, BuffModel>>(block, new ParameterExpression[] { left, right });
        ModEntry.ModMonitor.LogIfVerbose($"Sum function generated:\n{lambda.ToCSharpString()}");
        return lambda.CompileFast();
    });

    private static Type? AsBaseType(Type netfield)
    {
        if (netfield == typeof(NetInt))
        {
            return typeof(int);
        }

        if (netfield == typeof(NetFloat))
        {
            return typeof(float);
        }

        return netfield.GenericTypeArguments.FirstOrDefault();
    }

    #endregion

    /// <inheritdoc cref="BuffEffects.CombatLevel"/>
    public float CombatLevel { get; set; } = 0;

    /// <inheritdoc cref="BuffEffects.Immunity"/>
    public float Immunity { get; set; } = 0;

    /// <inheritdoc cref="BuffEffects.AttackMultiplier" />
    public float AttackMultiplier { get; set; } = 0f;

    /// <inheritdoc cref="BuffEffects.KnockbackMultiplier"/>
    public float KnockbackMultiplier { get; set; } = 0f;

    /// <inheritdoc cref="BuffEffects.WeaponSpeedMultiplier"/>
    public float WeaponSpeedMultiplier { get; set; } = 0f;

    /// <inheritdoc cref="BuffEffects.CriticalChanceMultiplier"/>
    public float CriticalChanceMultiplier { get; set; } = 0f;

    /// <inheritdoc cref="BuffEffects.CriticalPowerMultiplier"/>
    public float CriticalPowerMultiplier { get; set; } = 0f;

    /// <inheritdoc cref="BuffEffects.WeaponPrecisionMultiplier"/>
    /// <remarks>This is unused in vanilla.</remarks>
    public float WeaponPrecisionMultiplier { get; set; } = 0f;

    /// <summary>
    /// Performs a leftfold combine on two buff models.
    /// </summary>
    /// <param name="left">Left model.</param>
    /// <param name="right">Right model.</param>
    /// <returns>Left model, after getting right model combined into it.</returns>
    internal static BuffModel LeftFold(BuffModel left, BuffModel right)
    {
        _leftFold.Value(left, right);
        return left;
    }

    /// <summary>
    /// Generates a buff effect mirroring this data.
    /// </summary>
    /// <returns>The buff effect.</returns>
    internal BuffEffects ToBuffEffect()
        => this.Merge(new());

    /// <summary>
    /// Merges this data with an existing buff effect.
    /// </summary>
    /// <param name="other">Buff effect to add to.</param>
    /// <returns>The buff effect.</returns>
    internal BuffEffects Merge(BuffEffects other)
    {
        if (other is null)
        {
            return null!;
        }
        _merger.Value(other, this);
        return other;
    }

    /// <summary>
    /// Gets the number of extra rows this buff model will take up in the tooltip.
    /// </summary>
    /// <returns>Number of extra rows.</returns>
    internal int GetExtraRows() => _extraRows.Value(this);
}

/// <summary>
/// A model to hold the light-ring like effects for a ring.
/// </summary>
public sealed class LightData
{
    /// <summary>
    /// Gets or sets the color of the light.
    /// </summary>
    public Color Color { get; set; } = new(0, 50, 170);

    /// <summary>
    /// Gets or sets the radius of the light.
    /// </summary>
    public int Radius { get; set; } = -1;
}

/// <summary>
/// The attributes associated with displaying a <see cref="EquipmentBuffTrigger.OnMonsterSlay"/> buff to the player.
/// Ignored for <see cref="EquipmentBuffTrigger.OnEquip"/> buffs.
/// </summary>
public sealed class BuffDisplayAttributes
{
    /// <summary>
    /// Gets or sets the texture to display.
    /// </summary>
    public string? Texture { get; set; }

    /// <summary>
    /// Gets or sets the index of that texture.
    /// </summary>
    public int SpriteIndex { get; set; } = -1;

    /// <summary>
    /// Gets or sets the duration of the buff (in ms).
    /// </summary>
    public int Duration { get; set; } = Game1.realMilliSecondsPerGameMinute * 10;

    /// <summary>
    /// Gets or sets a tokenized string for the display name of a buff, or null to use the ring's name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets a tokenized string for the description of a buff, or null to use the ring's description.
    /// </summary>
    public string? Description { get; set; }
}