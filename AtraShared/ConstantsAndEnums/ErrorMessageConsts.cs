namespace AtraShared.ConstantsAndEnums;

/// <summary>
/// Static class that contains common error messages.
/// </summary>
internal static class ErrorMessageConsts
{
#if HARMONY
    internal const string HARMONYCRASH = "Mod crashed while applying harmony patches:\n\n{0}";
#endif
}