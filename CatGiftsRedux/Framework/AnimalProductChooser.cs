using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.ItemManagement;

using AtraShared.Utils.Extensions;
using AtraShared.Wrappers;

using StardewValley.Objects;

namespace CatGiftsRedux.Framework;

/// <summary>
/// Tries to pick a random animal product.
/// </summary>
internal static class AnimalProductChooser
{
    internal static Item? Pick(Random random)
    {
        ModEntry.ModMonitor.DebugOnlyLog("Picked Animal Products");

        Dictionary<string, string>? content = Game1.content.Load<Dictionary<string, string>>("Data\\FarmAnimals");
        if (content.Count == 0)
        {
            return null;
        }

        int tries = 3;
        do
        {
            KeyValuePair<string, string> randomAnimal = content.ElementAt(random.Next(content.Count));
            if (int.TryParse(randomAnimal.Value.GetNthChunk('/', 2), out int id) && id > 0)
            {
                // confirm the item exists.
                if (!Game1Wrappers.ObjectInfo.TryGetValue(id, out string? objectData)
                    || objectData.GetNthChunk('1', SObject.objectInfoNameIndex).Contains("Qi", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (DataToItemMap.IsActuallyRing(id))
                {
                    return new Ring(id);
                }

                return new SObject(id, 1);
            }
        }
        while (tries-- > 0);
        return null;
    }
}
