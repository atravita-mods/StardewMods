#if caching

using System.Runtime.Caching;

namespace AtraShared.ItemManagement;

public static class ItemHandler
{
    private static readonly MemoryCache cache = new(typeof(ItemHandler).FullName!);


}

#endif