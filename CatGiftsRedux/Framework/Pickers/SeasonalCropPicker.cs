using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.StringHandler;

using AtraCore.Framework.ItemManagement;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;

using StardewValley.Extensions;
using StardewValley.GameData.Crops;
using StardewValley.Objects;

namespace CatGiftsRedux.Framework.Pickers;

/// <summary>
/// Picks a random seasonal crop item.
/// </summary>
internal static class SeasonalCropPicker
{
    internal static Item? Pick(Random random)
    {
        ModEntry.ModMonitor.DebugOnlyLog("Picked Seasonal Crops");

        List<CropData> content = Game1.cropData.Values.Where(static crop => crop.Seasons.Contains(Game1.season)).ToList();

        if (content.Count == 0)
        {
            return null;
        }

        int tries = 3;
        do
        {
            CropData entry = content[random.Next(content.Count)];
            string id = entry.HarvestItemId;

            // confirm the item exists.
            if (Utils.ForbiddenFromRandomPicking(id))
            {
                continue;
            }

            if (entry.TintColors?.Count > 0)
            {
                Color? color = Utility.StringToColor(random.ChooseFrom(entry.TintColors));
                if (color is not null)
                {
                    return new ColoredObject(id, 1, color.Value);
                }
            }

            Item? candidate = ItemRegistry.Create(ItemRegistry.type_object + id);
            if (candidate is not null)
            {
                return candidate;
            }
        }
        while (tries-- > 0);

        return null;
    }
}
