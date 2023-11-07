using System.Collections.Concurrent;
using AtraBase.Toolkit;
using AtraShared.Niceties;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;

namespace AtraCore.Framework.Caches.AssetCache;
public static class AssetCache
{
    private static readonly ConcurrentDictionary<string, WeakReference<IAssetHolder<object>>> Cache = new();
    private static readonly ConcurrentDictionary<string, List<WeakReference<IAssetHolder<object>>>> Managed = new();
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
    internal static BaseAssetHolder<TAsset>? Get<TAsset>(string? key)
        where TAsset : class
    {
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        IAssetName parsed = gameContent.ParseAssetName(key);
        return Get<TAsset>(parsed);
    }

    /// <summary>
    /// Gets an asset associated with an <see cref="IAssetName"/>.
    /// </summary>
    /// <param name="parsed">parsed asset name.</param>
    /// <returns>AssetHolder, or null if it could not be resolved.</returns>
    internal static BaseAssetHolder<TAsset>? Get<TAsset>(IAssetName parsed)
        where TAsset : class
    {
        {
            if (Cache.TryGetValue(parsed.BaseName, out WeakReference<IAssetHolder<object>>? weakref)
                && weakref.TryGetTarget(out IAssetHolder<object>? holder))
            {
                if (holder is BaseAssetHolder<TAsset> result)
                {
                    return result;
                }
                TKThrowHelper.ThrowInvalidCastException($"{holder.GetType().FullDescription()} could not be cast to {typeof(BaseAssetHolder<TAsset>).FullDescription()}");
            }
        }

        if (Failed.Contains(parsed))
        {
            ModEntry.ModMonitor.DebugOnlyLog($"{parsed.Name} marked fail in AssetCache, skipping.", LogLevel.Info);
            return null;
        }

        try
        {
            ModEntry.ModMonitor.DebugOnlyLog($"Trying to load {parsed.Name} in AssetCache.", LogLevel.Info);

            if (typeof(TAsset).IsAssignableTo(typeof(Texture2D)))
            {
                TextureHolder holder = new(parsed);
                Texture2D? texture = holder.Value;

                if (texture is null)
                {
                    return null;
                }
                if (texture.IsDisposed)
                {
                    Failed.Add(parsed);
                    DelayedAction.functionAfterDelay(() => gameContent.InvalidateCacheAndLocalized(parsed), 20);
                }
                Cache[string.IsInterned(parsed.BaseName) ?? parsed.BaseName] = new(holder);
                return holder as BaseAssetHolder<TAsset>;
            }
            else
            {
                BaseAssetHolder<TAsset> holder = new(parsed);
                Cache[string.IsInterned(parsed.BaseName) ?? parsed.BaseName] = new(holder);
            }
        }
        catch (ContentLoadException)
        {
            Failed.Add(parsed);
            ModEntry.ModMonitor.Log($"Asset {parsed} does not exist, skipping.", LogLevel.Warn);
        }
        catch (Exception ex)
        {
            Failed.Add(parsed);
            ModEntry.ModMonitor.LogError($"loading {parsed}", ex);
            throw;
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
            foreach ((string? key, WeakReference<IAssetHolder<object>>? holder) in Cache)
            {
                if (!holder.TryGetTarget(out IAssetHolder<object>? target))
                {
                    Cache.TryRemove(key, out _);
                }
                else
                {
                    target.MarkDirty();
                }
            }

            Failed.Clear();
        }
        else
        {
            foreach (IAssetName asset in assets)
            {
                if (Cache.TryGetValue(asset.BaseName, out WeakReference<IAssetHolder<object>>? holder))
                {
                    if (!holder.TryGetTarget(out IAssetHolder<object>? target))
                    {
                        Cache.TryRemove(asset.BaseName, out _);
                    }
                    else
                    {
                        target.MarkDirty();
                    }
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
        if (Cache.TryGetValue(asset.BaseName, out WeakReference<IAssetHolder<object>>? holder))
        {
            if (holder.TryGetTarget(out IAssetHolder<object>? target))
            {
                target.MarkDirty();
            }
            else
            {
                Cache.TryRemove(asset.BaseName, out _);
            }
        }
        Failed.Remove(asset);
    }
}
