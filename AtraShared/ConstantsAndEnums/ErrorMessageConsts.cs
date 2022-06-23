namespace AtraShared.ConstantsAndEnums;

/// <summary>
/// Static class that contains common error messages.
/// </summary>
public static class ErrorMessageConsts
{
#if HARMONY
    public const string HARMONYCRASH = "Mod crashed while applying harmony patches:\n\n{0}";
#endif
}