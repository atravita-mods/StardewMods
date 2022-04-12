namespace GiantCropFertilizer.DataModels;

/// <summary>
/// Data model used to save the ID number, to protect against shuffling...
/// </summary>
public class GiantCropFertilizerIDStorage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GiantCropFertilizerIDStorage"/> class.
    /// </summary>
    /// <remarks>This constructor is for Newtonsoft.</remarks>
    public GiantCropFertilizerIDStorage()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GiantCropFertilizerIDStorage"/> class.
    /// </summary>
    /// <param name="id">ID to save.</param>
    public GiantCropFertilizerIDStorage(int id)
        => this.ID = id;

    /// <summary>
    /// Gets or sets the ID number to store.
    /// </summary>
    public int ID { get; set; } = 0;
}