// Ignore Spelling: Knockback

namespace AtraCore.Framework.Models;

using AtraBase.Toolkit;

using Microsoft.Xna.Framework;

using StardewValley.Buffs;
using StardewValley.GameData.Objects;

/// <summary>
/// A model to hold the light-ring like effects for a ring.
/// </summary>
/// <param name="Color">The color to use.</param>
/// <param name="Radius"></param>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopErrorConsts.IsRecord)]
public sealed record LightData(Color Color, int Radius = -1)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LightData"/> class that corresponds to default data.
    /// </summary>
    public LightData()
        : this(new Color(0, 50, 170))
    {
    }
}

/// <summary>
/// The attributes associated with displaying a <see cref="RingBuffTrigger.OnMonsterSlay"/> buff to the player.
/// Ignored for <see cref="RingBuffTrigger.OnEquip"/> buffs.
/// </summary>
/// <param name="Texture">The texture of the buff, or null to use default.</param>
/// <param name="SpriteIndex">The index of the sprite.</param>
/// <param name="Duration">How long the buff should last for.</param>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopErrorConsts.IsRecord)]
public record BuffDisplayAttributes(string? Texture, int SpriteIndex, int Duration)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BuffDisplayAttributes"/> class.
    /// </summary>
    public BuffDisplayAttributes()
        : this(null, 0, Game1.realMilliSecondsPerGameMinute * 10)
    {
    }
}

/// <summary>
/// The possible times to trigger a buff from the ring.
/// </summary>
[Flags]
public enum RingBuffTrigger
{
    /// <summary>
    /// Buffs are added when the ring is equipped.
    /// </summary>
    OnEquip = 0b1,

    /// <summary>
    /// Buffs are added when a monster is slain when the ring is equipped.
    /// </summary>
    OnMonsterSlay = 0b10,
}

/// <summary>
/// A data model representing valid ring effects.
/// </summary>
public sealed class RingExtModel
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
    public List<RingEffects> Effects { get; set; } = new();

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
    internal RingEffects? GetEffect(RingBuffTrigger trigger, GameLocation? location, Farmer? player)
    {
        location ??= Game1.currentLocation;
        player ??= Game1.player;

        if (location is null || player is null)
        {
            return null;
        }

        foreach (RingEffects effect in this.Effects)
        {
            if (effect.Trigger.HasFlag(trigger)
                && GameStateQuery.CheckConditions(effect.Condition, location, player, random: Random.Shared, ignoreQueryKeys: trigger == RingBuffTrigger.OnEquip ? NonDeterministicQueries : null))
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
public sealed class RingEffects
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
    /// Gets or sets how to display buffs for <see cref="RingBuffTrigger.OnMonsterSlay"/>.
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
    /// Gets or sets a value indicating when to apply the buffs from this ring.
    /// </summary>
    public RingBuffTrigger Trigger { get; set; } = RingBuffTrigger.OnEquip;
}

/// <summary>
/// A model that represents possible buffs to the player.
/// </summary>
public sealed class BuffModel : ObjectBuffAttributesData
{
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
}