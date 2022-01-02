namespace SpecialOrdersExtended;
/// <summary>
/// Thrown when I recieve a value to an enum I didn't expect
/// </summary>
/// <typeparam name="T">The enum</typeparam>
public class UnexpectedEnumValueException<T> : Exception
{
    public UnexpectedEnumValueException(T value)
        : base($"Enum {typeof(T).Name} recieved unexpected value {value}")
    {
    }
}

/// <summary>
/// Thrown when a save is not loaded and I expect one to be
/// </summary>
public class SaveNotLoadedError : Exception
{
    public SaveNotLoadedError() :
        base("Save not loaded")
    {
    }
}
