using Microsoft.Xna.Framework.Graphics;

using StardewValley.Objects;

namespace DresserMiniMenu.Framework.MiniFarmerMenuIcons;

internal abstract class BaseColorFilter
{
    protected const int SIZE = 32; // px

    internal virtual void draw(SpriteBatch b, bool selected)
    {

    }

    internal abstract bool Filter(Clothing clothing);
}
