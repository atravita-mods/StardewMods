using System.Reflection;

using StardewValley.Delegates;

namespace SinZsEventTester.Framework;

/// <summary>
/// Tests GSQ.
/// </summary>
/// <param name="monitor">The monitor instance to use.</param>
/// <param name="reflector">SMAPI's reflection helper.</param>
internal sealed class GSQTester(IMonitor monitor, IReflectionHelper reflector)
{
    private static readonly Dictionary<string, Func<string, bool>> _additionalAssets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Data/MineCarts"] = static name => Extensions.IsPossibleGSQString(name) || name is "MinecartsUnlocked",
        ["Data/Characters"] = static name => Extensions.IsPossibleGSQString(name) || name is "CanSocialize" or "CanVisitIsland" or "ItemDeliveryQuest" or "WinterStarParticipant" or "MinecartsUnlocked",
    };

    private readonly SObject puffer = new("420", 1);

    /// <inheritdoc cref="IEventTesterAPI.RegisterAsset(IAssetName, Func{string, bool}?)"/>
    internal static bool Register(IAssetName asset, Func<string, bool>? filter)
        => _additionalAssets.TryAdd(asset.BaseName, filter ?? Extensions.IsPossibleGSQString);

    /// <inheritdoc cref="IEventTesterAPI.RegisterAsset(IAssetName, HashSet{string})"/>
    internal static bool Register(IAssetName asset, HashSet<string> additional)
        => _additionalAssets.TryAdd(asset.BaseName, name => Extensions.IsPossibleGSQString(name) || additional.Contains(name));

    /// <inheritdoc cref="IEventTesterAPI.RemoveAsset(IAssetName)"/>
    internal static bool Remove(IAssetName assets)
        => _additionalAssets.Remove(assets.BaseName);

    /// <summary>
    /// Checks <see cref="DataLoader"/>'s assets' GSQ.
    /// </summary>
    /// <param name="content">The localized content manager to use.</param>
    internal void Check(LocalizedContentManager content)
    {
        if (!Context.IsWorldReady)
        {
            monitor.Log($"A save has not been loaded. Some queries may not resolve correctly.", LogLevel.Warn);
        }

        foreach (MethodInfo method in typeof(DataLoader).GetMethods())
        {
            ParameterInfo[] p = method.GetParameters();
            if (p.Length == 1 && p[0].ParameterType == typeof(LocalizedContentManager))
            {
                object? data = method.Invoke(null, [content]);
                if (data is null)
                {
                    continue;
                }

                string asset = $"Data/{method.Name.Replace('_', '/')}";
                string[] breadcrumbs = [asset];

                this.Process(data, breadcrumbs, _additionalAssets.GetValueOrDefault(asset) ?? Extensions.IsPossibleGSQString);
            }
        }

        // create a new asset manager to avoid poisoning the one we're given.
        LocalizedContentManager tempAssetManager = content.CreateTemporary();
        try
        {
            foreach ((string asset, Func<string, bool> gsqfilter) in _additionalAssets)
            {
                if (asset.StartsWith("Data", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                object? data = tempAssetManager.Load<object>(asset);
                if (data is null)
                {
                    continue;
                }

                this.Process(data, [asset], gsqfilter);
            }
        }
        finally
        {
            tempAssetManager.Dispose();
        }
    }

    private void Process(object data, string[] breadcrumbs, Func<string, bool> filter)
    {
        if (data is null)
        {
            return;
        }

        Type t = data.GetType();

        if (t.IsGenericType)
        {
            Type[] types = t.GetGenericArguments();

            Type dataType = types.Last();
            if (dataType == typeof(string) || (dataType.IsValueType && dataType.AssemblyQualifiedName?.Contains("System", StringComparison.OrdinalIgnoreCase) == true))
            {
                monitor.VerboseLog($"{breadcrumbs.Render()} appears to be a simple asset, skipping.");
                return;
            }

            if (t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                Type tkey = types.First();
                MethodInfo processor = this.GetType().GetMethod(nameof(this.ProcessDictionary), BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(tkey, dataType)!;
                processor.Invoke(this, [data, breadcrumbs, filter]);
            }
            else if (t.GetGenericTypeDefinition() == typeof(List<>))
            {
                MethodInfo processor = this.GetType().GetMethod(nameof(this.ProcessList), BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(dataType);
                processor.Invoke(this, [data, breadcrumbs, filter]);
            }
            else
            {
                throw new InvalidDataException($"Type {t} was not expected in data at {breadcrumbs.Render()}.");
            }
        }
        else
        {
            foreach (FieldInfo field in t.GetFields())
            {
                if (filter(field.Name) && field.FieldType == typeof(string))
                {
                    string? gsq = (string?)field.GetValue(data);
                    this.CheckGSQ(gsq, [..breadcrumbs, field.Name]);
                }
                else if (!field.FieldType.IsValueType && (field.FieldType.IsGenericType || field.FieldType.Assembly!.GetName()!.Name!.Contains("StardewValley", StringComparison.OrdinalIgnoreCase)))
                {
                    object f = field.GetValue(data)!;
                    this.Process(f, [.. breadcrumbs, field.Name], filter);
                }
            }

            foreach (PropertyInfo prop in t.GetProperties())
            {
                if (filter(prop.Name) && prop.PropertyType == typeof(string))
                {
                    string? gsq = (string?)prop.GetValue(data);
                    this.CheckGSQ(gsq, [..breadcrumbs, prop.Name]);
                }
                else if (!prop.PropertyType.IsValueType && (prop.PropertyType.IsGenericType || prop.PropertyType.Assembly!.GetName()!.Name!.Contains("StardewValley", StringComparison.OrdinalIgnoreCase)))
                {
                    object f = prop.GetValue(data)!;
                    this.Process(f, [.. breadcrumbs, prop.Name], filter);
                }
            }
        }
    }

    private void ProcessDictionary<TKey, TValue>(Dictionary<TKey, TValue> data, string[] breadcrumbs, Func<string, bool> filter)
        where TKey : notnull
    {
        foreach ((TKey k, TValue v) in data)
        {
            this.Process(v!, [.. breadcrumbs, k.ToString()!], filter);
        }
    }

    private void ProcessList<T>(List<T> data, string[] breadcrumbs, Func<string, bool> filter)
    {
        for (int i = 0; i < data.Count; i++)
        {
            object? v = data[i];
            if (v is null)
            {
                continue;
            }

            string? id = (reflector.GetField<string>(v, "Id", false) ?? reflector.GetField<string>(v, "ID", false))?.GetValue();
            id ??= (reflector.GetProperty<string>(v, "Id", false) ?? reflector.GetProperty<string>(v, "ID", false))?.GetValue();
            id ??= i.ToString()!;

            this.Process(v!, [.. breadcrumbs, id], filter);
        }
    }

    private void CheckGSQ(string? gsq, string[] breadcrumbs)
    {
        if (string.IsNullOrEmpty(gsq))
        {
            return;
        }

        monitor.Log($"Checking: {gsq}\n{breadcrumbs.Render()}", LogLevel.Info);

        if (gsq is "TRUE" or "FALSE")
        {
            return;
        }

        Farmer? player = Game1.player;
        GameLocation location = player?.currentLocation ?? Game1.currentLocation;
        GameStateQuery.ParsedGameStateQuery[] parsed = GameStateQuery.Parse(gsq);
        if (parsed.Length == 0)
        {
            return;
        }

        if (parsed[0].Error is { } error)
        {
            GameStateQuery.Helpers.ErrorResult(parsed[0].Query, error);
            return;
        }

        GameStateQueryContext context = new(location, player, this.puffer, this.puffer, Random.Shared);

        foreach (GameStateQuery.ParsedGameStateQuery query in parsed)
        {
            // the ANY query is checked separately.
            if (query.Query[0].Equals("ANY", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string? subquery in query.Query.AsSpan(1))
                {
                    this.CheckGSQ(subquery, [.. breadcrumbs, gsq]);
                }
                continue;
            }

            try
            {
                query.Resolver(query.Query, context);
            }
            catch (Exception ex)
            {
                monitor.Log($"Encountered exception running {string.Join(", ", query.Query)}, see log for details.", LogLevel.Error);
                monitor.Log(ex.ToString());
            }
        }
    }
}

/// <summary>
/// The extension methods for this class.
/// </summary>
file static class Extensions
{
    internal static string Render(this string[] breadcrumbs) => string.Join("->", breadcrumbs);

    internal static bool IsPossibleGSQString(this string name)
        => name.EndsWith("Condition");
}
