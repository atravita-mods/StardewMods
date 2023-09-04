using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtraCore.Framework.Caches.AssetCache;

/// <summary>
/// A holder for an asset.
/// </summary>
/// <typeparam name="TOutput">The type of the output.</typeparam>
public interface IAssetHolder<TOutput>
{
    public TOutput Value { get; }

    public IAssetName AssetName { get; }
}
