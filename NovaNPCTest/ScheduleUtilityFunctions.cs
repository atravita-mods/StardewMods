using StardewModdingAPI.Utilities;

using StardewValley.Network;

namespace NovaNPCTest;
internal class ScheduleUtilityFunctions
{
    private readonly IMonitor monitor;
    private readonly ITranslationHelper translation;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleUtilityFunctions"/> class.
    /// </summary>
    /// <param name="monitor">The logger.</param>
    /// <param name="translation">The translation helper.</param>
    public ScheduleUtilityFunctions(
        IMonitor monitor,
        ITranslationHelper translation)
    {
        this.monitor = monitor;
        this.translation = translation;
    }

    /// <summary>
    /// Given a raw schedule string, returns a new raw schedule string, after following the GOTO/MAIL/NOT friendship keys in the game.
    /// </summary>
    /// <param name="npc">NPC.</param>
    /// <param name="date">The data to analyze.</param>
    /// <param name="rawData">The raw schedule string.</param>
    /// <param name="scheduleString">A raw schedule string, stripped of MAIL/GOTO/NOT elements. Ready to be parsed.</param>
    /// <returns>True if successful, false for error (skip to next schedule entry).</returns>
    public bool TryFindGOTOschedule(NPC npc, SDate date, string rawData, out string scheduleString)
    {
        scheduleString = string.Empty;
        string[] splits = rawData.Split(
            separator: '/',
            count: 3,
            options: StringSplitOptions.TrimEntries);
        string[] command = splits[0].Split();
        switch (command[0])
        {
            case "GOTO":
                // GOTO NO_SCHEDULE
                if (command[1].Equals("NO_SCHEDULE", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                string newKey = command[1];

                // GOTO season
                if (newKey.Equals("Season", StringComparison.OrdinalIgnoreCase))
                {
                    newKey = date.SeasonKey.ToLowerInvariant();
                }

                // GOTO newKey
                if (npc.hasMasterScheduleEntry(newKey))
                {
                    string newscheduleKey = npc.getMasterScheduleEntry(newKey);
                    if (newscheduleKey.Equals(rawData, StringComparison.Ordinal))
                    {
                        this.monitor.Log(this.translation.Get("GOTO_INFINITE_LOOP").Default("Infinite loop detected, skipping this schedule."), LogLevel.Warn);
                        return false;
                    }
                    return this.TryFindGOTOschedule(npc, date, newscheduleKey, out scheduleString);
                }
                else
                {
                    this.monitor.Log(
                        this.translation.Get("GOTO_SCHEDULE_NOT_FOUND")
                        .Default("GOTO {{scheduleKey}} not found for NPC {{npc}}")
                        .Tokens(new { scheduleKey = newKey, npc = npc.Name }), LogLevel.Warn);
                    return false;
                }
            case "NOT":
                // NOT friendship NPCName heartLevel
                if (command[1].Equals("friendship", StringComparison.Ordinal))
                {
                    NPC? friendNpc = Game1.getCharacterFromName(command[2]);
                    if (friendNpc is null)
                    {
                        // can't find the friend npc.
                        this.monitor.Log(
                            this.translation.Get("GOTO_FRIEND_NOT_FOUND")
                            .Default("NPC {{npc}} not found, friend requirement {{requirment}} cannot be evaluated: {{scheduleKey}}")
                            .Tokens(new { npc = command[2], requirment = splits[0], schedulekey = rawData }), LogLevel.Warn);
                        return false;
                    }

                    int hearts = Utility.GetAllPlayerFriendshipLevel(friendNpc) / 250;
                    if (!int.TryParse(command[3], out int heartLevel))
                    {
                        // ill formed friendship check string, warn
                        this.monitor.Log(
                            this.translation.Get("GOTO_ILL_FORMED_FRIENDSHIP")
                            .Default("Ill-formed friendship requirment {{requirment}} for {{npc}}: {{scheduleKey}}")
                            .Tokens(new { requirment = splits[0], npc = npc.Name, scheduleKey = rawData }), LogLevel.Warn);
                        return false;
                    }
                    else if (hearts > heartLevel)
                    {
                        // hearts above what's allowed, skip to next schedule.
                        this.monitor.Log(
                            this.translation.Get("GOTO_SCHEDULE_FRIENDSHIP")
                            .Default("Skipping due to friendship limit for {{npc}}: {{scheduleKey}}")
                            .Tokens(new { npc = npc.Name, scheduleKey = rawData }), LogLevel.Trace);
                        return false;
                    }
                }
                scheduleString = rawData;
                return true;
            case "MAIL":
                // MAIL mailkey
                return Game1.MasterPlayer.mailReceived.Contains(command[1]) || NetWorldState.checkAnywhereForWorldStateID(command[1])
                    ? this.TryFindGOTOschedule(npc, date, splits[2], out scheduleString)
                    : this.TryFindGOTOschedule(npc, date, splits[1], out scheduleString);
            default:
                scheduleString = rawData;
                return true;
        }
    }
}
