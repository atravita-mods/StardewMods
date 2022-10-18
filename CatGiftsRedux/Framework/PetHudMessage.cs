using Microsoft.Xna.Framework;

namespace CatGiftsRedux.Framework;

/// <summary>
/// A custom subclass of the Hudmessage to draw in the pet's head and the item.
/// </summary>
internal sealed class PetHudMessage : HUDMessage
{
    private Item spawnedItem;

    /// <summary>
    /// Initializes a new instance of the <see cref="PetHudMessage"/> class.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="color"></param>
    /// <param name="timeLeft"></param>
    /// <param name="fadeIn"></param>
    /// <param name="spawnedItem"></param>
    public PetHudMessage(string message, Color color, float timeLeft, bool fadeIn, Item spawnedItem)
        : base(message, color, timeLeft, fadeIn)
    {
        this.spawnedItem = spawnedItem;
    }
}
