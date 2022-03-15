#if TRANSPILERS

/* **********************************
 * Don't forget to include COLLECTIONS!
 * **********************************/

// TODO: AssertIs?
// Label stuff?
// MAKE SURE THE LABEL COUNTS ARE RIGHT. Inserting codes should add to the Important Labels! Check **any time** labels are removed.

using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using AtraBase.Collections;
using AtraShared.Utils.Extensions;
using HarmonyLib;

namespace AtraShared.Utils.HarmonyHelper;

/// <summary>
/// Helper class for transpilers.
/// </summary>
public class ILHelper
{
    // Locals found via inspecting LocalBuilders.
    private readonly SortedList<int, LocalBuilder> builtLocals = new();

    // All locals.
    private readonly SortedList<int, LocalVariableInfo> locals = new();

    private readonly Counter<Label> importantLabels = new();

    private Label? label = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="ILHelper"/> class.
    /// </summary>
    /// <param name="original">Original method's methodbase.</param>
    /// <param name="codes">IEnumerable of codes.</param>
    /// <param name="monitor">Logger.</param>
    /// <param name="generator">ILGenerator.</param>
    public ILHelper(MethodBase original, IEnumerable<CodeInstruction> codes, IMonitor monitor, ILGenerator generator)
    {
        if (original.GetMethodBody() is MethodBody body)
        {
            foreach (LocalVariableInfo loc in body.LocalVariables)
            {
                this.locals[loc.LocalIndex] = loc;
            }
        }
        else
        {
            throw new InvalidOperationException($"Attempted to transpile a method without a body: {original.FullDescription()}");
        }
        this.Original = original;
        this.Codes = codes.ToList();
        this.Generator = generator;
        this.Monitor = monitor;

        foreach (CodeInstruction code in this.Codes)
        {
            if (code.operand is LocalBuilder builder)
            {
                this.builtLocals.TryAdd(builder.LocalIndex, builder);
                this.locals.TryAdd(builder.LocalIndex, builder); // LocalBuilder inherits from LocalVariableInfo
            }
            if (code.Branches(out Label? label))
            {
                this.importantLabels[label!.Value]++;
            }
        }
    }

    /// <summary>
    /// Gets the original methodbase.
    /// </summary>
    public MethodBase Original { get; init; }

    /// <summary>
    /// Gets the list of codes.
    /// </summary>
    /// <remarks>Try not to use this.</remarks>
    public List<CodeInstruction> Codes { get; init; }

    /// <summary>
    /// Gets the ILGenerator.
    /// </summary>
    public ILGenerator Generator { get; init; }

    /// <summary>
    /// Gets the current instruction pointer stack.
    /// </summary>
    public Stack<int> PointerStack { get; private set; } = new();

    /// <summary>
    /// Points to the current location in the instructions list.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:Property summary documentation should match accessors", Justification = "Reviewed.")]
    public int Pointer { get; private set; } = -1;

    /// <summary>
    /// Gets the current instruction.
    /// </summary>
    public CodeInstruction CurrentInstruction
    {
        get => this.Codes[this.Pointer];
        private set => this.Codes[this.Pointer] = value;
    }

    /// <summary>
    /// Gets the logger for this instance.
    /// </summary>
    protected IMonitor Monitor { get; init; }

    /// <summary>
    /// Pushes the pointer onto the pointerstack.
    /// </summary>
    /// <returns>this.</returns>
    public ILHelper Push()
    {
        this.PointerStack.Push(this.Pointer);
        return this;
    }

    /// <summary>
    /// Pops the a pointer from the pointerstack.
    /// </summary>
    /// <returns>this.</returns>
    public ILHelper Pop()
    {
        this.Pointer = this.PointerStack.Pop();
        return this;
    }

    // Todo: Consider doing basic stack checking here.

    /// <summary>
    /// The list of codes as an enumerable.
    /// </summary>
    /// <returns>Returns the list of codes as an enumerable.</returns>
    public IEnumerable<CodeInstruction> Render() =>
        this.Codes.AsEnumerable();

    /// <summary>
    /// Prints out the current codes to console.
    /// Only works in DEBUG.
    /// </summary>
    [Conditional("DEBUG")]
    public void Print()
    {
        StringBuilder sb = new();
        sb.Append("ILHelper for: ").AppendLine(this.Original.FullDescription());
        for (int i = 0; i < this.Codes.Count; i++)
        {
            sb.AppendLine().Append(this.Codes[i]);
            if (this.Pointer == i)
            {
                sb.Append("       <----");
            }
            if (this.PointerStack.Contains(i))
            {
                sb.Append("       <----- stack point.");
            }
        }
        this.Monitor.Log(sb.ToString(), LogLevel.Info);
    }

    /// <summary>
    /// Moves the pointer forward the number of steps.
    /// </summary>
    /// <param name="steps">Number of steps.</param>
    /// <returns>this.</returns>
    /// <exception cref="IndexOutOfRangeException">Pointer tried to move to an invalid location.</exception>
    public ILHelper Advance(int steps)
    {
        this.Pointer += steps;
        if (this.Pointer < 0 || this.Pointer >= this.Codes.Count)
        {
            throw new IndexOutOfRangeException("New location for pointer is out of bounds.");
        }
        return this;
    }

    /// <summary>
    /// Finds the first occurrence of the following pattern between the indexes given.
    /// </summary>
    /// <param name="instructions">Instructions to search for.</param>
    /// <param name="startindex">Index to start searching at (inclusive).</param>
    /// <param name="intendedendindex">Index to end search (exclusive). Null for "end of instruction list".</param>
    /// <returns>this.</returns>
    /// <exception cref="ArgumentException">Startindex or Endindex are invalid.</exception>
    /// <exception cref="IndexOutOfRangeException">No match found.</exception>
    public ILHelper FindFirst(CodeInstructionWrapper[] instructions, int startindex = 0, int? intendedendindex = null)
    {
        int endindex = intendedendindex ?? this.Codes.Count;
        if (startindex >= (endindex - instructions.Length) || startindex < 0 || endindex > this.Codes.Count)
        {
            throw new ArgumentException($"Either startindex {startindex} or endindex {endindex} are invalid. ");
        }

        for (int i = startindex; i < endindex - instructions.Length; i++)
        {
            bool found = true;
            for (int j = 0; j < instructions.Length; j++)
            {
                if (!instructions[j].Matches(this.Codes[i + j]))
                {
                    found = false;
                    break;
                }
            }
            if (found)
            {
                this.Pointer = i;
                return this;
            }
        }
        this.Monitor.Log($"The desired pattern wasn't found:\n\n" + string.Join('\n', instructions.Select(i => i.ToString())), LogLevel.Error);
        throw new IndexOutOfRangeException();
    }

    /// <summary>
    /// Finds the next occurrence of the code instruction.
    /// </summary>
    /// <param name="instructions">Instructions to search for.</param>
    /// <returns>this.</returns>
    /// <exception cref="ArgumentException">Fewer codes remain than the length of the instructions to search for.</exception>
    /// <exception cref="IndexOutOfRangeException">No match found.</exception>
    public ILHelper FindNext(CodeInstructionWrapper[] instructions)
        => this.FindFirst(instructions, this.Pointer + 1, this.Codes.Count);

    /// <summary>
    /// Finds the last occurrence of the following pattern between the indexes given.
    /// </summary>
    /// <param name="instructions">Instructions to search for.</param>
    /// <param name="startindex">Index to start searching at (inclusive).</param>
    /// <param name="intendedendindex">Index to end search (exclusive). Leave null to mean "last code".</param>
    /// <returns>this.</returns>
    /// <exception cref="ArgumentException">Startindex or Endindex are invalid.</exception>
    /// <exception cref="IndexOutOfRangeException">No match found.</exception>
    public ILHelper FindLast(CodeInstructionWrapper[] instructions, int startindex = 0, int? intendedendindex = null)
    {
        int endindex = intendedendindex ?? this.Codes.Count;
        if (startindex >= endindex - instructions.Length || startindex < 0 || endindex > this.Codes.Count)
        {
            throw new ArgumentException($"Either startindex {startindex} or endindex {endindex} are invalid. ");
        }

        for (int i = endindex - instructions.Length - 1; i >= startindex; i--)
        {
            bool found = true;
            for (int j = 0; j < instructions.Length; j++)
            {
                if (!instructions[j].Matches(this.Codes[i + j]))
                {
                    found = false;
                    break;
                }
            }
            if (found)
            {
                this.Pointer = i;
                return this;
            }
        }
        this.Monitor.Log($"The desired pattern wasn't found:\n\n" + string.Join('\n', instructions.Select(i => i.ToString())), LogLevel.Error);
        throw new IndexOutOfRangeException();
    }

    /// <summary>
    /// Finds the previous occurrence of the code instruction.
    /// </summary>
    /// <param name="instructions">Instructions to search for.</param>
    /// <returns>this.</returns>
    /// <exception cref="ArgumentException">Fewer codes remain than the length of the instructions to search for.</exception>
    /// <exception cref="IndexOutOfRangeException">No match found.</exception>
    public ILHelper FindPrev(CodeInstructionWrapper[] instructions)
        => this.FindLast(instructions, 0, this.Pointer);

    /// <summary>
    /// Inserts the following code instructions at this location.
    /// </summary>
    /// <param name="instructions">Instructions to insert.</param>
    /// <param name="withLabels">Labels to attach to the first instruction.</param>
    /// <returns>this.</returns>
    public ILHelper Insert(CodeInstruction[] instructions, IList<Label>? withLabels = null)
    {
        this.Codes.InsertRange(this.Pointer, instructions);
        if (withLabels is not null)
        {
            this.CurrentInstruction.labels.AddRange(withLabels);
        }
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Branches(out Label? label))
            {
                this.importantLabels[label!.Value]++;
            }
        }
        this.Pointer += instructions.Length;
        return this;
    }

    /// <summary>
    /// Removes the following number of instructions.
    /// </summary>
    /// <param name="count">Number to remove.</param>
    /// <returns>this.</returns>
    /// <exception cref="InvalidOperationException">Attempted to remove an important label, stopping.</exception>
    public ILHelper Remove(int count)
    {
        for (int i = this.Pointer; i < this.Pointer + count; i++)
        {
            if (this.Codes[i].Branches(out Label? label))
            {
                this.importantLabels[label!.Value]--;
            }
        }
        this.importantLabels.RemoveZeros();
        for (int i = this.Pointer; i < this.Pointer + count; i++)
        {
            if (this.Codes[i].labels.Intersect(this.importantLabels.Keys).Any())
            {
                StringBuilder sb = new();
                sb.Append("Attempted to remove an important label!\n\nThis code's labels: ")
                    .AppendJoin(", ", this.Codes[i].labels.Select(l => l.ToString()))
                    .AppendLine().Append("Important labels: ")
                    .AppendJoin(", ", this.importantLabels.Select(l => l.ToString()));
                this.Monitor.Log(sb.ToString(), LogLevel.Error);
                throw new InvalidOperationException();
            }
        }
        this.Codes.RemoveRange(this.Pointer, count);
        return this;
    }

    /// <summary>
    /// Removes instructions until it encounters the *first* instruction of a specific pattern.
    /// </summary>
    /// <param name="instructions">List of instructions to search for.</param>
    /// <returns>this.</returns>
    /// <exception cref="InvalidOperationException">Attempted to remove an important label, stopping.</exception>
    public ILHelper RemoveUntil(CodeInstructionWrapper[] instructions)
    {
        this.Push();
        this.FindNext(instructions);
        int finalpos = this.Pointer;
        this.Pop();
        this.Remove(finalpos - this.Pointer);
        return this;
    }

    /// <summary>
    /// Removes instructions, up to and including the match pattern.
    /// </summary>
    /// <param name="instructions">List of instructions to search for.</param>
    /// <returns>this.</returns>
    /// <exception cref="InvalidOperationException">Attempted to remove an important label, stopping.</exception>
    public ILHelper RemoveIncluding(CodeInstructionWrapper[] instructions)
    {
        this.Push();
        this.FindNext(instructions);
        int finalpos = this.Pointer + instructions.Length;
        this.Pop();
        this.Remove(finalpos - this.Pointer);
        return this;
    }

    /// <summary>
    /// Replace the current instruction with the given instruction.
    /// </summary>
    /// <param name="instruction">Instruction to replace.</param>
    /// <param name="withLabels">Labels to attach, if any.</param>
    /// <param name="keepLabels">Whether or not to keep the original labels. Default: true.</param>
    /// <returns>this.</returns>
    public ILHelper ReplaceInstruction(CodeInstruction instruction, Label[] withLabels, bool keepLabels = true)
    {
        this.ReplaceInstruction(instruction, keepLabels);
        this.CurrentInstruction.labels.AddRange(withLabels);
        return this;
    }

    public ILHelper ReplaceInstruction(OpCode opcode, object operand, Label[] withLabels, bool keepLabels = true)
    {
        return this.ReplaceInstruction(new CodeInstruction(opcode, operand), withLabels, keepLabels);
    }

    public ILHelper ReplaceInstruction(CodeInstruction instruction, bool keepLabels = true)
    {
        if (keepLabels)
        {
            instruction.labels.AddRange(this.CurrentInstruction.labels);
        }
        else
        {
            this.importantLabels.RemoveZeros();
            if (this.CurrentInstruction.labels.Intersect(this.importantLabels.Keys).Any())
            {
                StringBuilder sb = new();
                sb.Append("Attempted to remove an important label!\n\nThis code's labels: ")
                    .AppendJoin(", ", this.CurrentInstruction.labels.Select(l => l.ToString()))
                    .AppendLine().Append("Important labels: ")
                    .AppendJoin(", ", this.importantLabels.Select(l => l.ToString()));
                this.Monitor.Log(sb.ToString(), LogLevel.Error);
                throw new InvalidOperationException();
            }
        }
        if (instruction.Branches(out Label? label))
        {
            this.importantLabels[label!.Value]++;
        }
        this.CurrentInstruction = instruction;
        return this;
    }

    public ILHelper ReplaceInstruction(OpCode opcode, object operand, bool keepLabels = true)
    {
        return this.ReplaceInstruction(new CodeInstruction(opcode, operand), keepLabels);
    }

    public ILHelper ReplaceOperand(object operand)
    {
        this.CurrentInstruction.operand = operand;
        return this;
    }

    public ILHelper GrabBranchDest(out Label? label)
    {
        if (!this.CurrentInstruction.Branches(out label))
        {
            throw new InvalidOperationException($"Attempted to grab label from something that's not a branch.");
        }
        return this;
    }

    public ILHelper StoreBranchDest()
    {
        return this.GrabBranchDest(out this.label);
    }

    /// <summary>
    /// Gets the labels from a certain instruction. (Primarily used for moving labels).
    /// </summary>
    /// <param name="labels">out labels.</param>
    /// <param name="clear">whether or not to clear the labels.</param>
    /// <returns>this.</returns>
    /// <remarks>DOES NOT CHECK LABELS! YOU SHOULD PROBABLY PUT THEM BACK SOMEWHERE if cleared.</remarks>
    public ILHelper GetLabels(out IList<Label> labels, bool clear = false)
    {
        labels = this.CurrentInstruction.labels.ToList();
        if (clear)
        {
            this.CurrentInstruction.labels.Clear();
        }
        return this;
    }

    public ILHelper AttachLabel(params Label[] labels)
    {
        this.CurrentInstruction.labels.AddRange(labels);
        return this;
    }

    public ILHelper DefineAndAttachLabel(out Label label)
    {
        label = this.Generator.DefineLabel();
        this.CurrentInstruction.labels.Add(label);
        return this;
    }

    public ILHelper AdvanceToLabel(Label label)
    {
        for (int i = this.Pointer; i < this.Codes.Count; i++)
        {
            if (this.Codes[i].labels.Contains(label))
            {
                this.Pointer = i;
                return this;
            }
        }
        throw new IndexOutOfRangeException($"label {label} could not be found after index {this.Pointer}");
    }

    public ILHelper AdvanceToStoredLabel()
    {
        if (this.label is null)
        {
            throw new InvalidOperationException("Attempted to advance to label, but there is not one stored!");
        }
        return this.AdvanceToLabel(this.label.Value);
    }

    /// <summary>
    /// Gets the index of the local of a specifc type.
    /// </summary>
    /// <param name="type">Type to search for.</param>
    /// <param name="which">If there's multiple locals of a single type, which one.</param>
    /// <returns>Index of the local, -1 if not found.</returns>
    public int GetIndexOfLocal(Type type, int which = 1)
    {
        int counter = 0;
        foreach ((int key, LocalVariableInfo local) in this.locals)
        {
            if (local.LocalType == type && ++counter == which)
            {
                return local.LocalIndex;
            }
        }
        return -1;
    }

    // 90% sure this is in Harmony already.....

    /// <summary>
    /// Gets the instruction for loading a local at the index.
    /// Short form only. (so less than 255 local variables).
    /// </summary>
    /// <param name="localindex">Index of the local to get.</param>
    /// <returns>The proper local instruction.</returns>
    public static CodeInstruction GetLdLoc(int localindex)
    {
        if (localindex > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException($"Recieved localindex too large - {localindex}");
        }
        return localindex switch
        {
            0 => new(OpCodes.Ldloc_0),
            1 => new(OpCodes.Ldloc_1),
            2 => new(OpCodes.Ldloc_2),
            3 => new(OpCodes.Ldloc_3),
            _ => new(OpCodes.Ldloc, localindex)
        };
    }

    /// <summary>
    /// Gets the instruction for storing to a local at the index.
    /// Short form only. (so less than 255 local variables).
    /// </summary>
    /// <param name="localindex">Index of the local to get.</param>
    /// <returns>The proper local instruction.</returns>
    public static CodeInstruction GetStLoc(int localindex)
    {
        if (localindex > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException($"Recieved localindex too large - {localindex}");
        }
        return localindex switch
        {
            0 => new(OpCodes.Stloc_0),
            1 => new(OpCodes.Stloc_1),
            2 => new(OpCodes.Stloc_2),
            3 => new(OpCodes.Stloc_3),
            _ => new(OpCodes.Stloc, localindex)
        };
    }
}

#endif