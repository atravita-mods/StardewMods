using System.Diagnostics;
using System.Runtime.CompilerServices;

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

/// <summary>
/// ThrowHelper for AtraShared exceptions.
/// </summary>
public static class ASThrowHelper
{
    /// <summary>
    /// Throws a new SaveNotLoadedError.
    /// </summary>
    /// <exception cref="SaveNotLoadedError">always.</exception>
    [DoesNotReturn]
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    [StackTraceHidden]
    public static void ThrowSaveNotLoaded()
    {
        throw new SaveNotLoadedError();
    }

    /// <summary>
    /// Throws a new SaveNotLoadedError.
    /// </summary>
    /// <typeparam name="T">Type to return.</typeparam>
    /// <exception cref="SaveNotLoadedError">always.</exception>
    /// <returns>nothing, doesn't return.</returns>
    [DoesNotReturn]
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    [StackTraceHidden]
    public static T ThrowSaveNotLoaded<T>()
    {
        throw new SaveNotLoadedError();
    }
}