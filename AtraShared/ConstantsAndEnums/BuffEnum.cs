using AtraBase.Toolkit;
using AtraBase.Toolkit.Extensions;

using NetEscapades.EnumGenerators;

using StardewValley.Buffs;

namespace AtraShared.ConstantsAndEnums;

/// <summary>
/// An enum that corresponds to valid buffs in stardew.
/// </summary>
[Flags]
[EnumExtensions]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items should be documented", Justification = StyleCopErrorConsts.SelfEvident)]
public enum BuffEnum
{
    Farming = 1 << Buff.farming,
    Fishing = 1 << Buff.fishing,
    Mining = 1 << Buff.mining,
    Luck = 1 << Buff.luck,
    Foraging = 1 << Buff.foraging,
    MaxStamina = 1 << Buff.maxStamina,
    MagneticRadius = 1 << Buff.magneticRadius,
    Speed = 1 << Buff.speed,
    Defense = 1 << Buff.defense,
    Attack = 1 << Buff.attack,
}

/// <summary>
/// Extensions for <see cref="BuffEnum"/>.
/// </summary>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:Field names should not begin with underscore", Justification = "Preference.")]
public static partial class BuffEnumExtensions
{
    private static readonly Random _random = new Random().PreWarm();
    private static readonly BuffEnum[] _all = BuffEnumExtensions.GetValues();

    public static BuffEnum GetRandomBuff(Random? random = null)
    {
        random ??= _random;
        return _all[random.Next(Length)];
    }

    public static Buff GetBuffOf(
        this BuffEnum buffEnum,
        int amount,
        int minutesDuration,
        string source,
        string displaySource,
        string? id = null,
        string? displayName = null,
        string? description = null)
    {
        BuffEffects effects = new();

        if (buffEnum.HasFlagFast(BuffEnum.Farming))
        {
            effects.FarmingLevel.Value = amount;
        }

        if (buffEnum.HasFlagFast(BuffEnum.Fishing))
        {
            effects.FishingLevel.Value = amount;
        }

        if (buffEnum.HasFlagFast(BuffEnum.Mining))
        {
            effects.MiningLevel.Value = amount;
        }

        if (buffEnum.HasFlagFast(BuffEnum.Luck))
        {
            effects.LuckLevel.Value = amount;
        }

        if (buffEnum.HasFlagFast(BuffEnum.Foraging))
        {
            effects.ForagingLevel.Value = amount;
        }

        if (buffEnum.HasFlagFast(BuffEnum.MaxStamina))
        {
            effects.MaxStamina.Value = amount * 10;
        }

        if (buffEnum.HasFlagFast(BuffEnum.MagneticRadius))
        {
            effects.MagneticRadius.Value = amount * 32;
        }

        if (buffEnum.HasFlagFast(BuffEnum.Speed))
        {
            effects.Speed.Value = amount;
        }

        if (buffEnum.HasFlagFast(BuffEnum.Defense))
        {
            effects.Defense.Value = amount;
        }

        if (buffEnum.HasFlagFast(BuffEnum.Attack))
        {
            effects.Attack.Value = amount;
        }

        if (id is null)
        {
            Span<byte> buffer = stackalloc byte[16];
            Random.Shared.NextBytes(buffer);
            id = Convert.ToBase64String(buffer);
        }

        return new Buff(
            id,
            source,
            displaySource,
            minutesDuration * Game1.realMilliSecondsPerGameMinute,
            null,
            0,
            effects,
            false,
            displayName,
            description);
    }
}
