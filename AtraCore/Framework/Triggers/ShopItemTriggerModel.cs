using Microsoft.Xna.Framework;

namespace AtraCore.Framework.Triggers;

/// <summary>
/// The data model corresponding to shop item triggers.
/// </summary>
public sealed class ShopItemTriggerModel
{
    public string? Texture { get; set; }

    public Rectangle SourceRect { get; set; }

    public List<string>? Actions { get; set; }

    /// <summary>
    /// A tokenized string representing the display name.
    /// </summary>
    public string? DisplayName { get; set; }
    public int Price { get; set; }
    public string? Description { get; set; }
}
