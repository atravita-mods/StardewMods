namespace AtraShared.Niceties;

/// <summary>
/// A comparer for <see cref="ISalable"/> sorting that sorts by display name, locale dependent.
/// </summary>
public sealed class SalableNameComparer : Comparer<ISalable>
{
    private readonly StringComparer _comparer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SalableNameComparer"/> class.
    /// </summary>
    /// <param name="comparer">the string comparer to use.</param>
    public SalableNameComparer(StringComparer comparer) => this._comparer = comparer;

    /// <inheritdoc />
    public override int Compare(ISalable? x, ISalable? y) => this._comparer.Compare(x?.DisplayName, y?.DisplayName);
}