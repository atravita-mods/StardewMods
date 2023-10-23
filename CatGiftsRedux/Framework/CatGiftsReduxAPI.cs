namespace CatGiftsRedux.Framework;

/// <inheritdoc />
public class CatGiftsReduxAPI : ICatGiftReduxAPI
{
    private readonly ModEntry us;
    private readonly IModInfo them;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatGiftsReduxAPI"/> class.
    /// </summary>
    /// <param name="us">This mod.</param>
    /// <param name="them">Their ModInfo.</param>
    internal CatGiftsReduxAPI(ModEntry us, IModInfo them)
    {
        this.us = us;
        this.them = them;
    }

    /// <inheritdoc />
    public void AddPicker(Func<Random, Item?> picker, double weight = 100)
    {
        if (weight > 0)
        {
            this.us.Monitor.Log($"Adding picker from: {this.them.Manifest.UniqueID}");
            this.us.AddPicker(weight, picker);
        }
        else
        {
            this.us.Monitor.Log($"Skipping picker from: {this.them.Manifest.UniqueID}, weight must be positive.");
        }
    }
}
