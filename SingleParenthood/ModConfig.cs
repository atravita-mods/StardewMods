namespace SingleParenthood;
internal sealed class ModConfig
{
    private int gestation = 14;

    /// <summary>
    /// Gets or sets how long an adoption/birth takes.
    /// </summary>
    public int Gestation
    {
        get => this.gestation;
        set => this.gestation = Math.Clamp(value, 1, 28);
    }

    private int maxKids = 2;

    public int MaxKids
    {
        get => this.maxKids;
        set => this.maxKids = Math.Clamp(value, 0, this.maxKids);
    }
}
