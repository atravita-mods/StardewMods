namespace AtraShared;

/// <summary>
/// Thrown when a save is not loaded but I expect one to be.
/// </summary>
public class SaveNotLoadedError : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaveNotLoadedError"/> class.
    /// </summary>
    public SaveNotLoadedError()
        : base("Save not loaded")
    {
    }
}