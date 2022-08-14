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

    public int Width { get; init; }

    public int Height { get; init; }

    public RawDataRented(Color[] data, int width, int height)
    {
        this.data = data;
        this.Width = width;
        this.Height = height;
    }

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

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            ArrayPool<Color>.Shared.Return(this.data);
            this.disposed = true;
        }
    }

    ~RawDataRented()
    {
         this.Dispose(disposing: false);
    }

    public void Dispose()
    {
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
