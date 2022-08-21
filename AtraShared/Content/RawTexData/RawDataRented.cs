using System.Buffers;
using CommunityToolkit.Diagnostics;
using Microsoft.Xna.Framework;

namespace AtraShared.Content.RawTextData;

/// <summary>
/// An implementation of IRawTextureData that uses
/// array pooling.
/// </summary>
/// <remarks>As usual with array pooling, array may be longer than necessary.</remarks>
public class RawDataRented : IRawTextureData, IDisposable
{
    private bool disposed = false;
    private Color[] data;

    /// <summary>
    /// Initializes a new instance of the <see cref="RawDataRented"/> class.
    /// </summary>
    /// <param name="data">Data. (expects a rented array).</param>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    public RawDataRented(Color[] data, int width, int height)
    {
        Guard.IsNotNull(data);
        Guard.IsGreaterThan(width, 0);
        Guard.IsGreaterThan(height, 0);

        this.data = data;
        this.Width = width;
        this.Height = height;
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="RawDataRented"/> class.
    /// </summary>
    ~RawDataRented()
    {
        this.Dispose(disposing: false);
    }

    /// <inheritdoc />
    public Color[] Data
    {
        get
        {
            if (this.disposed)
            {
                ThrowHelper.ThrowObjectDisposedException(nameof(RawDataRented));
                return default;
            }
            return this.data;
        }
    }

    /// <inheritdoc />
    public int Width { get; init; }

    /// <inheritdoc />
    public int Height { get; init; }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes a <see cref="RawDataRented" /> by returning the internal <see cref="ArrayPool{Color}" />.
    /// </summary>
    /// <param name="disposing">Whether this was called by the <see cref="Dispose" /> method or the finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            ArrayPool<Color>.Shared.Return(this.data);
            this.data = null!;
            this.disposed = true;
        }
    }
}
