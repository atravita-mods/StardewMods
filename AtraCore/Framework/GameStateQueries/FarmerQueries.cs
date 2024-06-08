using AtraShared.ConstantsAndEnums;

using StardewValley.Delegates;
using StardewValley.Objects;

using static StardewValley.GameStateQuery;

namespace AtraCore.Framework.GameStateQueries;
internal static class FarmerQueries
{
    private static readonly Dictionary<EquipmentType, Func<Farmer, string, bool>> _delegates = new()
    {
        [EquipmentType.Hat] = (farmer, qid) => farmer.hat.Value?.QualifiedItemId == qid,
        [EquipmentType.Ring] = (farmer, qid) => farmer.leftRing.Value.IsOrContainsId(qid) || farmer.rightRing.Value.IsOrContainsId(qid),
        [EquipmentType.Boots] = (farmer, qid) => farmer.boots.Value?.QualifiedItemId == qid,
        [EquipmentType.Pants] = (farmer, qid) => farmer.pantsItem.Value?.QualifiedItemId == qid,
        [EquipmentType.Shirt] = (farmer, qid) => farmer.shirtItem.Value?.QualifiedItemId == qid,
        [EquipmentType.Trinket] = (farmer, qid) => farmer.trinketItems.Any(t => t?.QualifiedItemId == qid),
    };

    /// <summary>
    /// A query to check if a player is wearing a specific item.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="context">The query context.</param>
    /// <returns>True if the player is wearing the item, false otherwise.</returns>
    internal static bool IsWearing(string[] query, GameStateQueryContext context)
    {
        if (!ArgUtility.TryGet(query, 1, out var playerKey, out string error)
            || !ArgUtility.TryGet(query, 2, out var equipType, out error)
            || !ArgUtility.TryGet(query, 3, out var qid, out error))
        {
            return Helpers.ErrorResult(query, error);
        }

        if (!EquipmentTypeExtensions.TryParse(equipType, out EquipmentType equipment, ignoreCase: true))
        {
            return Helpers.ErrorResult(query, $"could not parse {equipType} as valid equipment slot.");
        }

        if (!_delegates.TryGetValue(equipment, out var del))
        {
            return Helpers.ErrorResult(query, $"{equipType} does not correspond to a known equipment type.");
        }

        return Helpers.WithPlayer(
            context.Player,
            playerKey,
            (Farmer target) => del(target, qid));
    }

    private static bool IsOrContainsId(this Ring? ring, string qid)
    {
        if (ring is null)
        {
            return false;
        }
        if (ring.QualifiedItemId == qid)
        {
            return true;
        }
        if (ring is CombinedRing combined)
        {
            foreach (Ring? r in combined.combinedRings)
            {
                if (r.IsOrContainsId(qid))
                {
                    return true;
                }
            }
        }
        return false;
    }
}