namespace AtraShared.Integrations.Interfaces.Automate;

public interface IRecipe
{
    /// <summary>Matches items that can be used as input.</summary>
    Func<Item, bool> Input { get; }

    /// <summary>The number of inputs needed.</summary>
    int InputCount { get; }

    /// <summary>The output to generate (given an input).</summary>
    Func<Item, Item> Output { get; }

    /// <summary>The time needed to prepare an output (given an input).</summary>
    Func<Item, int> Minutes { get; }

    /// <summary>Get whether the recipe can accept a given item as input (regardless of stack size).</summary>
    /// <param name="stack">The item to check.</param>
    bool AcceptsInput(ITrackedStack stack);
}
