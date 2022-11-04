using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoreFertilizers.Framework;
internal static class RadioactiveFertilizerHandler
{
    private static IAssetName crops = null!;
    private static IAssetName objects = null!;

    internal static void Initialize(IGameContentHelper parser)
    {
        crops = parser.ParseAssetName("Data/Crops");
        objects = parser.ParseAssetName("Data/ObjectInformation");
    }

    internal static void Reset(IReadOnlySet<IAssetName> assets)
    {

    }
}
