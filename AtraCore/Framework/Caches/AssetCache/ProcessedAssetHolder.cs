using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtraCore.Framework.Caches.AssetCache;
internal class ProcessedAssetHolder<TAsset, TOutput> : IAssetHolder<TOutput>
{
    public TOutput Value { get; }
    public IAssetName AssetName { get; }
}
