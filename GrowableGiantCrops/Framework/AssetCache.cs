﻿using System.Collections.Concurrent;

using AtraShared.Niceties;

using StardewModdingAPI.Events;

using Microsoft.Xna.Framework.Graphics;

namespace GrowableGiantCrops.Framework;

/// <summary>
/// Handles caching and invalidating assets for this mod.
/// </summary>
internal static class AssetCache
{
    private static readonly ConcurrentDictionary<string, WeakReference<AssetHolder>> Cache = new();
    private static readonly HashSet<IAssetName> Failed = new(BaseAssetNameComparer.Instance);

    private static IGameContentHelper gameContent = null!;

    /// <summary>
    /// Initialize the asset cache.
    /// </summary>
    /// <param name="gameContentHelper">smapi's game content helper.</param>
    internal static void Initialize(IGameContentHelper gameContentHelper)
    {
        gameContent = gameContentHelper;
    }

    /// <summary>
    /// Gets an asset associated with a string asset path.
    /// </summary>
    /// <param name="key">string asset path.</param>
    /// <returns>AssetHolder, or null if could not be resolved.</returns>
    internal static AssetHolder? Get(string? key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        IAssetName parsed = gameContent.ParseAssetName(key);
        return Get(parsed);
    }

    /// <summary>
    /// Gets an asset associated with an <see cref="IAssetName"/>.
    /// </summary>
    /// <param name="parsed">parsed asset name.</param>
    /// <returns>AssetHolder, or null if it could not be resolved.</returns>
    internal static AssetHolder? Get(IAssetName parsed)
    {
        if (Cache.TryGetValue(parsed.BaseName, out WeakReference<AssetHolder>? weakref) && weakref.TryGetTarget(out AssetHolder? holder))
        {
            return holder;
        }

        if (Failed.Contains(parsed))
        {
            return null;
        }

        try
        {
            Texture2D texture = gameContent.Load<Texture2D>(parsed);
            if (!texture.IsDisposed)
            {
                AssetHolder newHolder = new(parsed, texture);
                Cache[parsed.BaseName] = new (newHolder);
                return newHolder;
            }
        }
        catch (Exception ex)
        {
            Failed.Add(parsed);
            ModEntry.ModMonitor.LogOnce($"Failed to load {parsed}.", LogLevel.Error);
            ModEntry.ModMonitor.Log(ex.ToString());
        }

        return null;
    }

    /// <summary>
    /// Propagates an <see cref="IContentEvents.AssetsInvalidated" /> to the cached assets.
    /// </summary>
    /// <param name="assets">Assets to refresh, or null for all.</param>
    internal static void Refresh(IReadOnlySet<IAssetName>? assets)
    {
        if (assets is null)
        {
            foreach ((string? key, WeakReference<AssetHolder>? holder) in Cache)
            {
                if (!holder.TryGetTarget(out AssetHolder? _))
                {
                    Cache.TryRemove(key, out _);
                }
            }

            Failed.Clear();
        }
        else
        {
            foreach (IAssetName asset in assets)
            {
                if (Cache.TryGetValue(asset.BaseName, out WeakReference<AssetHolder>? holder) && !holder.TryGetTarget(out _))
                {
                    Cache.TryRemove(asset.BaseName, out WeakReference<AssetHolder> _);
                }
                Failed.Remove(asset);
            }
        }
    }

    /// <summary>
    /// Listens to when an asset is ready to refresh asset holders and clear failed loads.
    /// </summary>
    /// <param name="asset">the asset that is ready.</param>
    internal static void Ready(IAssetName asset)
    {
        if (Cache.TryGetValue(asset.BaseName, out WeakReference<AssetHolder>? holder))
        {
            if (holder.TryGetTarget(out AssetHolder? target))
            {
                target.Refresh();
            }
            else
            {
                Cache.TryRemove(asset.BaseName, out WeakReference<AssetHolder> _);
            }
        }
        Failed.Remove(asset);
    }
}
