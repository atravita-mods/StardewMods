namespace AtraShared.ConstantsAndEnums;

using AtraBase.Toolkit;

using CommunityToolkit.Diagnostics;

using NetEscapades.EnumGenerators;

using static System.Numerics.BitOperations;

/// <summary>
/// Skills as flags....
/// </summary>
[Flags]
[EnumExtensions]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items should be documented", Justification = StyleCopErrorConsts.SelfEvident)]
public enum Skills
{
    /// <summary>
    /// No skills.
    /// </summary>
    None = 0,

    /// <summary>
    /// Farming.
    /// </summary>
    Farming = 0b1 << Farmer.farmingSkill,
    Mining = 0b1 << Farmer.miningSkill,
    Fishing = 0b1 << Farmer.fishingSkill,
    Foraging = 0b1 << Farmer.foragingSkill,
    Combat = 0b1 << Farmer.combatSkill,
    Luck = 0b1 << Farmer.luckSkill,
}

/// <summary>
/// Extensions for the Skills enum.
/// </summary>
public static partial class SkillsExtensions
{
    private static readonly Skills[] _all = GetValues().Where(a => PopCount((uint)a) == 1).ToArray();

    /// <summary>
    /// Gets a span containing all vanilla skills.
    /// </summary>
    public static ReadOnlySpan<Skills> All => new(_all);

    /// <summary>
    /// Checks if this specific farmer has a specific skill level.
    /// </summary>
    /// <param name="farmer">Farmer to check.</param>
    /// <param name="skills">Skill to check for.</param>
    /// <param name="includeBuffs">Whether or not to include buffs.</param>
    /// <returns>True if that farmer has this wallet item.</returns>
    public static int GetSkillLevelFromEnum(this Farmer farmer, Skills skills, bool includeBuffs = true)
    {
        Guard.IsEqualTo(PopCount((uint)skills), 1);

        return skills switch
        {
            Skills.Mining => includeBuffs ? farmer.MiningLevel : farmer.miningLevel.Value,
            Skills.Farming => includeBuffs ? farmer.FarmingLevel : farmer.farmingLevel.Value,
            Skills.Foraging => includeBuffs ? farmer.ForagingLevel : farmer.foragingLevel.Value,
            Skills.Combat => includeBuffs ? farmer.CombatLevel : farmer.combatLevel.Value,
            Skills.Fishing => includeBuffs ? farmer.FishingLevel : farmer.fishingLevel.Value,
            Skills.Luck => includeBuffs ? farmer.LuckLevel : farmer.luckLevel.Value,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<int>($"{skills.ToStringFast()} does not correspond to a single vanilla skill!"),
        };
    }

    /// <summary>
    /// Converts from a Skills enum to the game constant.
    /// </summary>
    /// <param name="skills">The skill enum.</param>
    /// <returns>The game constant.</returns>
    public static int ToGameConstant(this Skills skills)
    {
        Guard.IsEqualTo(PopCount((uint)skills), 1);
        return skills switch
        {
            Skills.Farming => Farmer.farmingSkill,
            Skills.Mining => Farmer.miningSkill,
            Skills.Foraging => Farmer.foragingSkill,
            Skills.Combat => Farmer.combatSkill,
            Skills.Fishing => Farmer.fishingSkill,
            Skills.Luck => Farmer.luckSkill,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<int>($"{skills.ToStringFast()} does not correspond to a single vanilla skill!"),
        };
    }
}