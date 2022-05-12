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
        => this.IDMap[Constants.SaveFolderName!] = id;

    /// <summary>
    /// Gets or sets the ID number to store.
    /// </summary>
    public Dictionary<string, int> IDMap { get; set; } = new();

    internal int ID
    {
        get => this.IDMap[Constants.SaveFolderName!];
        set => this.IDMap[Constants.SaveFolderName!] = value;
    }
}