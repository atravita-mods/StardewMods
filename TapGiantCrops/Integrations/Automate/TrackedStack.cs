using AtraShared.Integrations.Interfaces.Automate;

namespace TapGiantCrops.Integrations.Automate;

/// <inheritdoc />
public class TrackedStack : ITrackedStack
{
    private readonly Item item;
    private readonly Action onEmpty;
    private bool preventedEmptyStacks;

    public TrackedStack(Item item, Action onEmpty)
    {
        this.item = item;
        this.onEmpty = onEmpty;
    }

    /// <inheritdoc />
    public Item Sample
    {
        get
        {
            var sample = this.item.getOne();
            sample.Stack = this.item.Stack;
            return sample;
        }
    }

    /// <inheritdoc />
    public ItemType Type => ItemType.Object;

    /// <inheritdoc />
    public int Count => this.item.Stack;

    /// <inheritdoc />
    public void PreventEmptyStacks()
    {
        if (!this.preventedEmptyStacks)
        {
            this.item.Stack = Math.Max(this.Count - 1, 0);
        }
        this.preventedEmptyStacks = true;
    }

    /// <inheritdoc />
    public void Reduce(int count)
    {
        this.item.Stack -= count;
        if (!this.preventedEmptyStacks && this.item.Stack <= 0)
        {
            this.onEmpty();
        }
    }

    /// <inheritdoc />
    public Item? Take(int count)
    {
        if (count <= 0)
        {
            return null;
        }
        this.Reduce(count);
        Item? ret = this.item.getOne();
        ret.Stack = count;
        return ret;
    }
}
