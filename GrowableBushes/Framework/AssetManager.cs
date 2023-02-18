using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

namespace GrowableBushes.Framework;
internal static class AssetManager
{
    internal static IAssetName bushes { get; private set; } = null!;

    internal static void Initialize(IGameContentHelper parser)
    {
        bushes = parser.ParseAssetName("Mods/atravita/GrowableBushes/BushTAS");
    }

    internal static void Load(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(bushes))
        {
            e.LoadFromModFile<Texture2D>("assets/shop.png", AssetLoadPriority.Exclusive);
        }
    }
}
