#if TRANSPILERS

using System.Reflection.Emit;
using HarmonyLib;

namespace AtraShared.Utils.HarmonyHelper;

/// <summary>
/// ILHelper was, for some reason, used incorrectly.
/// </summary>
internal class InvalidILHelperCommand : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidILHelperCommand"/> class.
    /// </summary>
    /// <param name="text">The text to show with the error.</param>
    internal InvalidILHelperCommand(string text)
        : base(text)
    {
    }
}

/// <summary>
/// Code instruction extensions on top of Harmony's included ones.
/// </summary>
internal static class AdditionalCodeInstructionExtensions
{
    /// <summary>
    /// Converts an instruction to the matching load local instruction.
    /// </summary>
    /// <param name="instruction">Instruction to convert.</param>
    /// <returns>Ldloc command.</returns>
    /// <exception cref="InvalidILHelperCommand">Could not convert to the ldloc.</exception>
    internal static CodeInstruction ToLdLoc(this CodeInstruction instruction)
    {
        OpCode code = instruction.opcode;
        if (code == OpCodes.Ldloc_0 || code == OpCodes.Stloc_0)
        {
            return new CodeInstruction(OpCodes.Ldloc_0);
        }
        else if (code == OpCodes.Ldloc_1 || code == OpCodes.Stloc_1)
        {
            return new CodeInstruction(OpCodes.Ldloc_1);
        }
        else if (code == OpCodes.Ldloc_2 || code == OpCodes.Stloc_2)
        {
            return new CodeInstruction(OpCodes.Ldloc_2);
        }
        else if (code == OpCodes.Ldloc_3 || code == OpCodes.Stloc_3)
        {
            return new CodeInstruction(OpCodes.Ldloc_3);
        }
        else if (code == OpCodes.Ldloc || code == OpCodes.Stloc)
        {
            return new CodeInstruction(OpCodes.Ldloc, instruction.operand);
        }
        else if (code == OpCodes.Ldloc_S || code == OpCodes.Ldloc_S)
        {
            return new CodeInstruction(OpCodes.Ldloc_S, instruction.operand);
        }
        else if (code == OpCodes.Ldloca || code == OpCodes.Ldloca_S)
        {
            return instruction.Clone();
        }
        throw new InvalidILHelperCommand($"Could not make ldloc from {instruction}");
    }

    /// <summary>
    /// Converts an instruction to the matching store local instruction.
    /// </summary>
    /// <param name="instruction">Instruction to convert.</param>
    /// <returns>Stloc command.</returns>
    /// <exception cref="InvalidILHelperCommand">Could not convert to the ldloc.</exception>
    internal static CodeInstruction ToStLoc(this CodeInstruction instruction)
    {
        OpCode code = instruction.opcode;
        if (code == OpCodes.Ldloc_0 || code == OpCodes.Stloc_0)
        {
            return new CodeInstruction(OpCodes.Stloc_0);
        }
        else if (code == OpCodes.Ldloc_1 || code == OpCodes.Stloc_1)
        {
            return new CodeInstruction(OpCodes.Stloc_1);
        }
        else if (code == OpCodes.Ldloc_2 || code == OpCodes.Stloc_2)
        {
            return new CodeInstruction(OpCodes.Stloc_2);
        }
        else if (code == OpCodes.Ldloc_3 || code == OpCodes.Stloc_3)
        {
            return new CodeInstruction(OpCodes.Stloc_3);
        }
        else if (code == OpCodes.Ldloc || code == OpCodes.Stloc)
        {
            return new CodeInstruction(OpCodes.Stloc, instruction.operand);
        }
        else if (code == OpCodes.Ldloc_S || code == OpCodes.Ldloc_S)
        {
            return new CodeInstruction(OpCodes.Stloc_S, instruction.operand);
        }
        throw new InvalidILHelperCommand($"Could not make stloc from {instruction}");
    }
}

#endif