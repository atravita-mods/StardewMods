using System.Buffers;
using CommunityToolkit.Diagnostics;
using Microsoft.Xna.Framework;

namespace AtraShared.Models;

/// <summary>
/// An implementation of IRawTextureData that uses
/// array pooling.
/// </summary>
/// <remarks>As usual with array pooling, array may be longer than necessary.</remarks>
internal class RawDataRented : IRawTextureData, IDisposable
{
    private bool disposed = false;
    private Color[] data;

    /// <summary>
    /// Initializes a new instance of the <see cref="RawDataRented"/> class.
    /// </summary>
    /// <param name="data">Data. (expectes a rented array).</param>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    public RawDataRented(Color[] data, int width, int height)
    {
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
                ThrowHelper.ThrowInvalidOperationException("Attempted to access a disposed RawDataRented");
                return default;
            }
            return this.data;
        }
    }

    /// <inheritdoc />
    public int Width { get; init; }

    /// <inheritdoc />
    public int Height { get; init; }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            ArrayPool<Color>.Shared.Return(this.data);
            this.disposed = true;
        }
    }

    public void Dispose()
    {
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
