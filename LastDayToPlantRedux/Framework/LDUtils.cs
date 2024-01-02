using AtraCore.Framework.ItemManagement;

using AtraShared.ConstantsAndEnums;
using AtraShared.Wrappers;

namespace LastDayToPlantRedux.Framework;
internal static class LDUtils
{
    /// <summary>
    /// Returns the id and type of an SObject, or null if not found.
    /// </summary>
    /// <param name="identifier">string identifier.</param>
    /// <returns>id/type tuple, or null for not found.</returns>
    internal static (string id, int type)? ResolveIDAndType(string identifier)
    {
        string? candidate = identifier;
        if (!Game1Wrappers.ObjectData.TryGetValue(identifier, out StardewValley.GameData.Objects.ObjectData? data))
        {
            candidate = DataToItemMap.GetID(ItemTypeEnum.SObject, identifier);
            if (candidate is null || !Game1Wrappers.ObjectData.TryGetValue(candidate, out data))
            {
                ModEntry.ModMonitor.Log($"{identifier} could not be resolved, skipping");
                return null;
            }
        }

        if (data.Category is not SObject.fertilizerCategory or SObject.SeedsCategory)
        {
            ModEntry.ModMonitor.Log($"{identifier} (id '{candidate}') does not appear to be a seed or fertilizer, skipping.");
            return null;
        }

        return (candidate, data.Category);
    }
}
