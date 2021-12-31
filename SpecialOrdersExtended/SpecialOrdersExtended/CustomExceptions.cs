namespace SpecialOrdersExtended;

public class UnexpectedEnumValueException<T> : Exception
{
    public UnexpectedEnumValueException(T value)
        : base($"Enum {typeof(T).Name} recieved unexpected value {value}")
    {
    }
}

public class SaveNotLoadedError : Exception
{
    public SaveNotLoadedError() :
        base("Save not loaded")
    {
    }
}
