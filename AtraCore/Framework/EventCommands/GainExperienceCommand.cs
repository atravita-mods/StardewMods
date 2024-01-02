using AtraShared.ConstantsAndEnums;
using StardewValley.Delegates;
using static System.Numerics.BitOperations;

namespace AtraCore.Framework.EventCommands;
internal static class GainExperience
{
    /// <inheritdoc cref="EventCommandDelegate"/>
    internal static void Command(Event @event, string[] args, EventContext context)
    {
        if (!ArgUtility.TryGet(args, 1, out string? skillS, out string? error, false) || !ArgUtility.TryGetInt(args, 2, out int experience, out error))
        {
            @event.LogCommandErrorAndSkip(args, error);
            return;
        }

        if (!SkillsExtensions.TryParse(skillS, out Skills skill) || PopCount((uint)skill) != 1 || !SkillsExtensions.IsDefined(skill))
        {
            @event.LogCommandErrorAndSkip(args, $"'{skillS}' not parseable as a vanilla skill");
            return;
        }

        Game1.player.gainExperience(skill.ToGameConstant(), experience);
    }
}
