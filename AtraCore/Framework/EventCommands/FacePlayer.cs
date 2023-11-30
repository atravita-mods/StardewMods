namespace AtraCore.Framework.EventCommands;
internal static class FacePlayer
{
    internal static void FacePlayerCommand(Event @event, string[] args, EventContext context)
    {
        if (!ArgUtility.TryGet(args, 1, out var actorName, out var error) || !ArgUtility.TryGetInt(args, 2, out var milliseconds, out error))
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

        if (!ArgUtility.TryGetOptional(args, 3, out var farmerName, out error, null, false))
        {
            context.LogErrorAndSkip(error);
            return;
        }

        int actorId = -1;
        if (farmerName is not null && !@event.IsFarmerActorId(farmerName, out var proposed))
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
