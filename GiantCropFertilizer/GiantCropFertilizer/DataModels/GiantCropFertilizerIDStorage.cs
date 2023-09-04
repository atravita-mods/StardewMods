namespace GiantCropFertilizer.DataModels;

/// <summary>
/// Data model used to save the ID number, to protect against shuffling...
/// </summary>
public sealed class GiantCropFertilizerIDStorage
{
    /// <summary>
    /// The legacy name for the Giant Crop Fertilizer.
    /// </summary>
    internal const string LegacyItemName = "Giant Crop Fertilizer";

    /// <summary>
    /// The location in the save data where the object IDs were stored.
    /// </summary>
    internal const string SAVESTRING = "SavedObjectID";

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
        get => this.IDMap.TryGetValue(Constants.SaveFolderName!, out int val) ? val : -1;
        set => this.IDMap[Constants.SaveFolderName!] = value;
    }
}