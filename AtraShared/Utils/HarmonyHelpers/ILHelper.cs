#if TRANSPILERS

// Don't forget to reference System.Collections.NonGeneric!

using System.Reflection;
using System.Reflection.Emit;
using AtraShared.Utils.HarmonyHelpers;
using HarmonyLib;

namespace AtraShared.Utils.HarmonyHelper;

/// <summary>
/// Helper class for transpilers.
/// </summary>
public class ILHelper
{
    private readonly SortedList<int, LocalBuilder> builtLocals = new();

    private readonly HashSet<Label> importantLabels = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ILHelper"/> class.
    /// </summary>
    /// <param name="original">Original method's methodbase.</param>
    /// <param name="codes">IEnumerable of codes.</param>
    /// <param name="generator">ILGenerator.</param>
    public ILHelper(MethodBase original, IEnumerable<CodeInstruction> codes, ILGenerator? generator = null)
    {
        this.Original = original;
        this.Codes = codes.ToList();
        this.Generator = generator;

        foreach (CodeInstruction code in this.Codes)
        {
            if (code.operand is LocalBuilder builder)
            {
                this.builtLocals.TryAdd(builder.LocalIndex, builder);
                Console.WriteLine(builder.ToString());
            }
            if (code.Branches(out Label? label) && label is not null)
            {
                this.importantLabels.Add(label.Value);
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
    public ILGenerator? Generator { get; init; }

    /// <summary>
    /// Gets the current instruction pointer stack.
    /// </summary>
    public Stack<int> PointerStack { get; private set; } = new();
    

    /// <summary>
    /// Points to the current location in the instructions list.
    /// </summary>
    public int Pointer { get; private set; } = -1;

    /// <summary>
    /// Finds the first occurance of the following pattern between the indexes given.
    /// </summary>
    /// <param name="instructions">Instructions to search for.</param>
    /// <param name="startindex">Index to start searching at (inclusive).</param>
    /// <param name="endindex">Index to end search (exclusive).</param>
    /// <returns>this.</returns>
    /// <exception cref="ArgumentException">Startindex or Endindex are invalid.</exception>
    /// <exception cref="IndexOutOfRangeException">No match found.</exception>
    public ILHelper FindFirst(CodeInstructionWrapper[] instructions, int startindex, int endindex)
    {
        if (startindex >= endindex - instructions.Length || startindex < 0 || endindex >= this.Codes.Count)
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
        throw new IndexOutOfRangeException($"The desired pattern wasn't found:\n\n" + string.Join('\n', instructions.Select(i => i.ToString())));
    }

    /// <summary>
    /// Finds the last occurance of the following pattern between the indexes given.
    /// </summary>
    /// <param name="instructions">Instructions to search for.</param>
    /// <param name="startindex">Index to start searching at (inclusive).</param>
    /// <param name="endindex">Index to end search (exclusive).</param>
    /// <returns>this.</returns>
    /// <exception cref="ArgumentException">Startindex or Endindex are invalid.</exception>
    /// <exception cref="IndexOutOfRangeException">No match found.</exception>
    public ILHelper FindLast(CodeInstructionWrapper[] instructions, int startindex, int endindex)
    {
        if (startindex >= endindex - instructions.Length || startindex < 0 || endindex >= this.Codes.Count)
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
        throw new IndexOutOfRangeException($"The desired pattern wasn't found:\n\n" + string.Join('\n', instructions.Select(i => i.ToString())));
    }

    /// <summary>
    /// Inserts the followin code instructions at this location.
    /// </summary>
    /// <param name="instructions">Instructions to insert.</param>
    /// <returns>this.</returns>
    public ILHelper Insert(CodeInstruction[] instructions)
    {
        this.Codes.InsertRange(this.Pointer, instructions);
        this.Pointer += instructions.Length;
        return this;
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
        foreach ((int key, LocalBuilder local) in this.builtLocals)
        {
            if (local.LocalType == type && ++counter == which)
            {
                return local.LocalIndex;
            }
        }
        return -1;
    }

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