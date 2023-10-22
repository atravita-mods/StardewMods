// Ignore Spelling: Knockback

namespace AtraCore.Framework.Models;

using System.Reflection;

using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.Caches.AssetCache;
using AtraCore.Framework.ReflectionManager;

using FastExpressionCompiler.LightExpression;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Netcode;

using StardewValley.Buffs;
using StardewValley.GameData.Objects;

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
    public List<EquipEffects> Effects { get; set; } = new();

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
    private float healthRemainder = 0f;

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
    internal void AddRegen(Farmer farmer)
    {
        if (this.StaminaRegen > 0 && farmer.Stamina < farmer.MaxStamina)
        {
            float amount = Math.Min(this.StaminaRegen, farmer.MaxStamina - farmer.Stamina);
            farmer.Stamina += amount;
            farmer.currentLocation.debris.Add(new Debris(amount.RandomRoundProportional(), farmer.Position, Color.Green, 1f, farmer));
        }
        if (this.HealthRegen > 0 && farmer.health < farmer.maxHealth)
        {
            float amount = Math.Min(this.HealthRegen + this.healthRemainder, farmer.maxHealth - farmer.health);
            int toAdd = (int)amount;
            if (toAdd > 0)
            {
                farmer.health += toAdd;
                farmer.currentLocation.debris.Add(new Debris(toAdd, farmer.Position, Color.Green, 1f, farmer));
                this.healthRemainder = amount - toAdd;
            }
            else
            {
                this.healthRemainder = amount;
            }
        }
    }
}

/// <summary>
/// A model that represents possible buffs to the player.
/// </summary>
public sealed class BuffModel : ObjectBuffAttributesData
{
    private static readonly Lazy<Action<BuffEffects, BuffModel>> _merger = new(() =>
    {
        ParameterExpression effects = Expression.ParameterOf<BuffEffects>("effects");
        ParameterExpression model = Expression.ParameterOf<BuffModel>("model");

        List<Expression> expressions = new();

        foreach (FieldInfo field in typeof(BuffModel).GetFields())
        {
            MemberInfo member = typeof(BuffEffects).GetMember(field.Name).SingleOrDefault() ?? ReflectionThrowHelper.ThrowMethodNotFoundException<MemberInfo>(field.Name);
            MemberExpression getter = Expression.Field(model, field);
            switch (member)
            {
                case FieldInfo asField:
                    if (CheckTypeMatch(asField.FieldType, field.FieldType))
                    {
                        MemberExpression netField = Expression.Field(effects, asField);
                        MemberExpression value = Expression.Property(netField, asField.FieldType.GetCachedProperty("Value", ReflectionCache.FlagTypes.InstanceFlags));
                        BinaryExpression assign = Expression.Assign(value, Expression.Add(value, getter));
                        expressions.Add(assign);
                    }

                    break;
                case PropertyInfo asProperty:
                    if (CheckTypeMatch(asProperty.PropertyType, field.FieldType))
                    {
                        MemberExpression netField = Expression.Property(effects, asProperty);
                        MemberExpression value = Expression.Property(netField, asProperty.PropertyType.GetCachedProperty("Value", ReflectionCache.FlagTypes.InstanceFlags));
                        BinaryExpression assign = Expression.Assign(value, Expression.Add(value, getter));
                    }
                    break;
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
                    if (CheckTypeMatch(asField.FieldType, property.PropertyType))
                    {
                        MemberExpression netField = Expression.Field(effects, asField);
                        MemberExpression value = Expression.Property(netField, asField.FieldType.GetCachedProperty("Value", ReflectionCache.FlagTypes.InstanceFlags));
                        BinaryExpression assign = Expression.Assign(value, Expression.Add(value, getter));
                        expressions.Add(assign);
                    }

                    break;
                case PropertyInfo asProperty:
                    if (CheckTypeMatch(asProperty.PropertyType, property.PropertyType))
                    {
                        MemberExpression netField = Expression.Property(effects, asProperty);
                        MemberExpression value = Expression.Property(netField, asProperty.PropertyType.GetCachedProperty("Value", ReflectionCache.FlagTypes.InstanceFlags));
                        BinaryExpression assign = Expression.Assign(value, Expression.Add(value, getter));
                    }
                    break;
                default:
                    ReflectionThrowHelper.ThrowMethodNotFoundException(property.Name);
                    break;
            }
        }

        BlockExpression block = Expression.Block(expressions);
        var lambda = Expression.Lambda<Action<BuffEffects, BuffModel>>(block, new ParameterExpression[] { effects, model });
        ModEntry.ModMonitor.VerboseLog($"Ring merge function generated:\n{lambda.ToCSharpString()}");
        return lambda.CompileFast();
    });

    private static readonly Lazy<Func<BuffModel, int>> _extraRows = new(() =>
    {
        List<Expression> expressions = new();

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
        var lambda = Expression.Lambda<Func<BuffModel, int>>(block, new ParameterExpression[] { model });
        ModEntry.ModMonitor.VerboseLog($"Height function generated:\n{lambda.ToCSharpString()}");
        return lambda.CompileFast();
    });

    private static readonly Lazy<Action<BuffModel, BuffModel>> _leftFold = new(() =>
    {
        List<Expression> expressions = new();

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
        var lambda = Expression.Lambda<Action<BuffModel, BuffModel>>(block, new ParameterExpression[] { left, right });
        ModEntry.ModMonitor.VerboseLog($"Sum function generated:\n{lambda.ToCSharpString()}");
        return lambda.CompileFast();
    });

    /// <inheritdoc cref="BuffEffects.CombatLevel"/>
    public int CombatLevel { get; set; } = 0;

    /// <inheritdoc cref="BuffEffects.Immunity"/>
    public int Immunity { get; set; } = 0;

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

    private static bool CheckTypeMatch(Type buffEffectsField, Type buffModelField)
    {
        if (buffModelField == typeof(int) && buffEffectsField == typeof(NetInt))
        {
            return true;
        }
        if (buffModelField == typeof(float) && buffEffectsField == typeof(NetFloat))
        {
            return true;
        }

        return false;
    }
}

/// <summary>
/// A model to hold the light-ring like effects for a ring.
/// </summary>
public sealed class LightData
{
    public Color Color { get; set; } = new(0, 50, 170);

    public int Radius { get; set; } = -1;
}

/// <summary>
/// The attributes associated with displaying a <see cref="EquipmentBuffTrigger.OnMonsterSlay"/> buff to the player.
/// Ignored for <see cref="EquipmentBuffTrigger.OnEquip"/> buffs.
/// </summary>
public sealed class BuffDisplayAttributes
{
    public string? Texture { get; set; }

    public int SpriteIndex { get; set; } = -1;

    public int Duration { get; set; } = Game1.realMilliSecondsPerGameMinute * 10;

    /// <summary>
    /// A tokenized string for the display name of a buff, or null to use the ring's name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// A tokenized string for the description of a buff, or null to use the ring's description.
    /// </summary>
    public string? Description { get; set; }
}