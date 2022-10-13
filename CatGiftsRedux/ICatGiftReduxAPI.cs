namespace CatGiftsRedux;

/// <summary>
/// The API for this mod.
/// </summary>
public interface ICatGiftReduxAPI
{
    /// <summary>
    /// Adds a picker with a specified weight.
    /// </summary>
    /// <param name="picker">A function that takes a random and returns an Item.</param>
    /// <param name="weight">Weight for the picker.</param>
    public void AddPicker(Func<Random, Item?> picker, double weight = 100);
}
