using System.Reflection;

using AtraBase.Toolkit.Reflection;

using CommunityToolkit.Diagnostics;

using HarmonyLib;

namespace AtraShared.Utils.Extensions;

#warning - possibly reconsider, you can possibly do something about jumps and get an heuristic for "is this necessarily a deterministic call or no".

/// <summary>
/// Extensions useful for reflection.
/// </summary>
public static class ReflectionExtensions
{
    /// <summary>
    /// Checks to see if an method originally calls base.
    /// </summary>
    /// <param name="method">Method to check.</param>
    /// <returns>true if any base call exists somewhere in the method, false otherwise.</returns>
    public static bool CallsBase(this MethodInfo method)
    {
        Guard.IsNotNull(method);

        if (method.GetBaseMethod() is not MethodInfo baseMethod)
        {
            return false;
        }

        return PatchProcessor.GetOriginalInstructions(method)
            .Any(instr => instr.Calls(baseMethod));
    }
}
