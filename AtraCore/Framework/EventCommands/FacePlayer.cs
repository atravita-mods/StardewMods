using StardewValley.Delegates;

namespace AtraCore.Framework.EventCommands;

/// <summary>
/// Turns the NPC to face the player.
/// </summary>
internal static class FacePlayer
{
    /// <inheritdoc cref="EventCommandDelegate"/>
    internal static void FacePlayerCommand(Event @event, string[] args, EventContext context)
    {
        if (!ArgUtility.TryGet(args, 1, out string? actorName, out string? error) || !ArgUtility.TryGetInt(args, 2, out int milliseconds, out error))
        {
            context.LogErrorAndSkip(error);
            return;
        }

        if (@event.getActorByName(actorName) is not NPC npc)
        {
            context.LogErrorAndSkip($"no NPC found with name '{actorName}'");
            Game1.eventFinished();
            return;
        }

        if (!ArgUtility.TryGetOptional(args, 3, out string? farmerName, out error, null, false))
        {
            context.LogErrorAndSkip(error);
            return;
        }

        int actorId = -1;
        if (farmerName is not null && !@event.IsFarmerActorId(farmerName, out int proposed))
        {
            actorId = proposed;
        }

        if (@event.GetFarmerActor(actorId) is Farmer farmer)
        {
            npc.facePlayer(farmer);
            npc.faceTowardFarmerTimer = milliseconds;
            npc.movementPause = milliseconds;
        }

        @event.CurrentCommand++;
    }
}
