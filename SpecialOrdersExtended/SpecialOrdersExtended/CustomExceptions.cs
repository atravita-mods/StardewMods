namespace SpecialOrdersExtended;

/// <summary>
/// Thrown when I recieve a value to an enum I didn't expect.
/// </summary>
/// <typeparam name="T">The enum.</typeparam>
public class UnexpectedEnumValueException<T> : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnexpectedEnumValueException{T}"/> class.
    /// </summary>
    /// <param name="value">The unexpected enum value.</param>
    public UnexpectedEnumValueException(T value)
        : base($"Enum {typeof(T).Name} recieved unexpected value {value}")
    {
    }
}

/// <summary>
/// Thrown when a save is not loaded and I expect one to be.
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
