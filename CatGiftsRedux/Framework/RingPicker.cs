using AtraCore.Framework.ItemManagement;

using AtraShared.ConstantsAndEnums;

using StardewValley.Objects;

namespace CatGiftsRedux.Framework;
internal static class RingPicker
{
    internal static Ring? Pick(Random random)
    {
        var possibilities = DataToItemMap.GetAll(ItemTypeEnum.Ring).ToList();

        var id = possibilities[random.Next(possibilities.Count)];

        return new Ring(id);
    }
}
