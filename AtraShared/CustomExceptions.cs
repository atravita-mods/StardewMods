namespace AtraShared;

/// <summary>
/// Thrown when I get an unexpected enum value.
/// </summary>
/// <typeparam name="T">The enum type that recieved an unexpected value.</typeparam>
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

/// <summary>
/// Thrown when a method accessed by reflection/Harmony isn't found.
/// </summary>
public class MethodNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MethodNotFoundException"/> class.
    /// </summary>
    /// <param name="methodname">Name of the method.</param>
    public MethodNotFoundException(string methodname)
        : base($"{methodname} not found!")
    {
    }
}