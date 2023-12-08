namespace NPCArrows.Framework.Monitors;

using AtraShared.Utils.Extensions;

/// <summary>
/// The generic friendship monitor.
/// </summary>
/// <param name="friendship">The friendship instance.</param>
/// <param name="npc">The npc.</param>
internal class BasicFriendshipMonitor(Friendship friendship, NPC npc)
    : AbstractFriendshipMonitor(friendship, npc)
{
    /// <inheritdoc />
    protected override void OnGiftGiven(int number) => ModEntry.ModMonitor.DebugOnlyLog($"{number} gifts given!", LogLevel.Alert);

    /// <inheritdoc />
    protected override void OnTalkedTo() => ModEntry.ModMonitor.DebugOnlyLog($"{this.npc.Name} talked to!", LogLevel.Alert);
}
