#if TRANSPILERS

using System.Reflection.Emit;
using HarmonyLib;

namespace AtraShared.Utils.HarmonyHelpers;

public enum SpecialCodeInstructionCases
{
    /// <summary>
    /// WildCard matches all codes.
    /// </summary>
    Wildcard,

    /// <summary>
    /// Matches all codes that load from an argument.
    /// </summary>
    LdArg,

    /// <summary>
    /// Matches all codes that store to an argument.
    /// </summary>
    StArg,

    /// <summary>
    /// Matches all codes that load the address of an argument.
    /// </summary>
    LdArgA,

    /// <summary>
    /// Matches all codes that load from a local.
    /// </summary>
    LdLoc,

    /// <summary>
    /// Matches all codes that store to a local.
    /// </summary>
    StLoc,
}

/// <summary>
/// Wraps the code instruction class of Harmony to allow for looser comparisons.
/// </summary>
public class CodeInstructionWrapper
{

    public SpecialCodeInstructionCases? SpecialInstructionCase { get; init; }

    public CodeInstruction? CodeInstruction { get; init; }

    private LocalBuilder? builder;
    private int? argumentPos;

    public CodeInstructionWrapper(OpCode opcode, object? operand = null)
        => this.CodeInstruction = new CodeInstruction(opcode, operand);

    public CodeInstructionWrapper(CodeInstruction instrution)
        => this.CodeInstruction = instrution;

    public CodeInstructionWrapper(SpecialCodeInstructionCases specialcase)
    {
        this.SpecialInstructionCase = specialcase;
    }

    public CodeInstructionWrapper(SpecialCodeInstructionCases specialcase, int argument)
    {
        if (specialcase is SpecialCodeInstructionCases.LdArg or SpecialCodeInstructionCases.StArg)
        {
            this.SpecialInstructionCase = specialcase;
            this.argumentPos = argument;
        }
        throw new ArgumentException("Argument position can only be used with LdArg or StArg");
    }

    public CodeInstructionWrapper(SpecialCodeInstructionCases specialcase, LocalBuilder builder)
    {
        if (specialcase is SpecialCodeInstructionCases.LdLoc or SpecialCodeInstructionCases.StLoc)
        {
            this.SpecialInstructionCase = specialcase;
            this.builder = builder;
        }
        throw new ArgumentException("Localbuilders can only beu sed with LdLoc or StLoc");
    }

    /// <summary>
    /// Whether or not this CodeInstructionWrapper is a valid match to the code instruction.
    /// </summary>
    /// <param name="instruction">Instruction to check against.</param>
    /// <returns>True for a match.</returns>
    /// <exception cref="UnexpectedEnumValueException{SpecialCodeInstructionCases}">Recieved an unexpeced enum value.</exception>
    public bool Matches(CodeInstruction instruction)
    {
        if (this.SpecialInstructionCase is null)
        {
            return this.CodeInstruction is not null && this.CodeInstruction.Is(instruction.opcode, instruction.operand);
        }
        return this.SpecialInstructionCase switch
        {
            SpecialCodeInstructionCases.Wildcard => true,
            SpecialCodeInstructionCases.LdArg => this.argumentPos is null ? instruction.IsLdarg() : instruction.IsLdarg(this.argumentPos),
            SpecialCodeInstructionCases.StArg => this.argumentPos is null ? instruction.IsStarg() : instruction.IsStarg(this.argumentPos),
            SpecialCodeInstructionCases.LdArgA => this.argumentPos is null ? instruction.IsLdarga() : instruction.IsLdarga(this.argumentPos),
            SpecialCodeInstructionCases.LdLoc => this.builder is null ? instruction.IsLdloc() : instruction.IsLdloc(this.builder),
            SpecialCodeInstructionCases.StLoc => this.builder is null ? instruction.IsStloc() : instruction.IsStloc(this.builder),
            _ => throw new UnexpectedEnumValueException<SpecialCodeInstructionCases>(this.SpecialInstructionCase.Value),
        };
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (this.SpecialInstructionCase is null)
        {
            if (this.CodeInstruction is null)
            {
                return "null CodeInstructionWrapper";
            }
            else
            {
                return this.CodeInstruction.opcode.Name + this.CodeInstruction.operand.ToString();
            }
        }
        else
        {
            return this.SpecialInstructionCase.Value.ToString();
        }
    }
}

#endif