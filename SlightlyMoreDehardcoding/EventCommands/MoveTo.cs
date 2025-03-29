
using Microsoft.Xna.Framework;

namespace SlightlyMoreDehardcoding.EventCommands;

/// <summary>
/// A class to move an npc to a specific location.
/// </summary>
internal static class MoveTo
{
    /// <summary>
    /// The four cardinal directions.
    /// </summary>
    private static readonly Point[] Directions = [new (1, 0), new (0, 1), new (-1, 0), new (0, -1)];

    private static readonly Queue<Node> queue = [];
    private static readonly HashSet<Point> visited = [];

    internal static void Command(Event @event, string[] args, EventContext context)
    {
        if (!ArgUtility.TryGet(args, 1, out var actorName, out var error)
            || !ArgUtility.TryGetPoint(args, 2, out var target, out error)
            || !ArgUtility.TryGetOptional(args, 4, out string direction_S, out error)
            || !ArgUtility.TryGetOptionalBool(args, 5, out var ignoreCollision, out error))
        {
            context.LogErrorAndSkip(error);
            return;
        }

        int direction = -1;
        if (direction_S is not null && !Utility.TryParseDirection(direction_S, out direction))
        {
            context.LogErrorAndSkip($"Could not parse direction {direction_S} (index 4) as a valid direction. Use a numeric value or 'up', 'down', 'left', or 'right'.");
            return;
        }

        // grab the right actor
        Character? c;
        if (@event.IsFarmerActorId(actorName, out int farmerNumber))
        {
            c = @event.GetFarmerActor(farmerNumber);
            if (c is null)
            {
                context.LogErrorAndSkip($"Could not find farmer by name {actorName}");
                return;
            }
        }
        else
        {
            c = @event.getActorByName(actorName, out bool isOptionalNpc);
            if (c is null)
            {
                context.LogErrorAndSkip($"no NPC found with name {actorName}", isOptionalNpc);
                return;
            }
        }

        target.X = @event.OffsetTileX(target.X);
        target.Y = @event.OffsetTileY(target.Y);

        // clear any existing controllers.
        @event.npcControllers.RemoveAll(controller => controller.puppet == c);

        // generate path.
        List<Vector2>? path = GeneratePath(c, target, ignoreCollision, Game1.currentLocation);

        if (path is null)
        {
            context.LogErrorAndSkip($"Could not find valid path from {c.TilePoint}");
            return;
        }

        if (direction > -1)
        {
            path.Add(new(direction, 250));
        }
        if (path.Count > 0)
        {
            @event.npcControllers.Add(new NPCController(c, path, false));
        }
        @event.CurrentCommand++;
    }

    private static List<Vector2>? GeneratePath(Character c, Point target, bool ignoreCollision, GameLocation location)
    {
        queue.Clear();
        visited.Clear();

        queue.Enqueue(new (null, c.TilePoint));
        visited.Add(c.TilePoint);

        while (queue.TryDequeue(out var current))
        {
            if (current.Tile == target)
            {
                return current.Unravel();
            }

            foreach (var d in Directions)
            {
                var next = current.Tile + d;

                if (ignoreCollision || IsPassable(location, next, c))
                {
                    queue.Enqueue(new (current, next));
                }
            }
        }

        return null;
    }

    private static bool IsPassable(GameLocation location, Point point, Character c)
    {
        // if not on map, false
        if (!location.isTileOnMap(point))
        {
            return false;
        }

        Vector2 tile = point.ToVector2();

        // if map blocks walking, return false.
        if (!location.isTilePassable(tile))
        {
            return false;
        }

        // if a terrain feature is in the way, return false.
        if (location.terrainFeatures.GetValueOrDefault(tile) is { } terrain && !terrain.isPassable(c))
        {
            return false;
        }

        if (location.getLargeTerrainFeatureAt(point.X, point.Y) is { } large && !large.isPassable(c))
        {
            return false;
        }

        // if furniture or an object is in the way.
        if (location.getObjectAtTile(point.X, point.Y, true) is { } obj)
        {
            return false;
        }

        // if a building is in the way
        if (location.buildings?.Count > 0)
        {
            var rectangle = c.GetBoundingBox();
            rectangle.X = point.X * 64;
            rectangle.Y = point.Y * 64;
            foreach (StardewValley.Buildings.Building? building in location.buildings)
            {
                if (building.intersects(rectangle))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private class Node
    {
        internal readonly Node? Prev;
        internal readonly Point Tile;
        internal readonly int Depth;

        internal Node(Node? prev, Point tile)
        {
            this.Prev = prev;
            this.Tile = tile;
            this.Depth = prev?.Depth is { } depth ? depth + 1 : 0;
        }

        internal List<Vector2> Unravel()
        {
            List<Vector2> result = [];

            Node workingNode = this;
            while (workingNode.Prev is not null)
            {
                Point delta = workingNode.Tile - workingNode.Prev.Tile;
                if (result.Count == 0)
                {
                    result.Add(delta.ToVector2());
                }
                else
                {
                    Vector2 prev = result.Last();
                    if ((prev.X == 0 && delta.X == 0) || (prev.Y == 0 && delta.Y == 0))
                    {
                        result[^1] = prev + delta.ToVector2();
                    }
                    else
                    {
                        result.Add(delta.ToVector2());
                    }
                }

                workingNode = workingNode.Prev;
            }

            result.Reverse();
            return result;
        }
    }
}
