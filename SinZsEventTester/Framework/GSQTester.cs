using System.Reflection;
using System.Reflection.PortableExecutable;

using Microsoft.Xna.Framework.Content;

using StardewValley.Delegates;
using StardewValley.GameData;
using StardewValley.GameData.Machines;
using StardewValley.Internal;

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
        ["Data/Characters"] = static name => Extensions.IsPossibleGSQString(name) || name is "CanSocialize" or "CanVisitIsland" or "ItemDeliveryQuest" or "WinterStarParticipant" or "MinecartsUnlocked" || name.StartsWith("Spouse"),
    };

    private readonly SObject puffer = new("128", 1);

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

    /// <summary>
    /// Checks the gsq of a single asset.
    /// </summary>
    /// <param name="content">Content manager (to copy from).</param>
    /// <param name="asset">Asset to check.</param>
    internal void Check(LocalizedContentManager content, string asset)
    {
        if (!Context.IsWorldReady)
        {
            monitor.Log($"A save has not been loaded. Some queries may not resolve correctly.", LogLevel.Warn);
        }

        // create a new asset manager to avoid poisoning the one we're given.
        LocalizedContentManager temp = content.CreateTemporary();

        try
        {
            object? data = temp.Load<object>(asset);
            if (data is null)
            {
                monitor.Log($"'{asset}' doesn't seem to exist.", LogLevel.Warn);
                return;
            }

            this.Process(data, [asset], _additionalAssets.GetValueOrDefault(asset) ?? Extensions.IsPossibleGSQString);
        }
        catch (ContentLoadException)
        {
            monitor.Log($"'{asset}' doesn't seem to exist.", LogLevel.Warn);
        }
        finally
        {
            temp.Dispose();
        }
    }

    private void Process(object data, string[] breadcrumbs, Func<string, bool> filter)
    {
        if (data is null)
        {
            return;
        }

        if (data is ISpawnItemData spawnable)
        {
            this.CheckItemSpawn(spawnable, breadcrumbs);
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

    private void CheckItemSpawn(ISpawnItemData spawnable, string[] breadcrumbs)
    {
        monitor.Log($"Checking: {spawnable.ItemId} - {spawnable.PerItemCondition ?? "no per item condition"}\n{breadcrumbs.Render()}", LogLevel.Info);

        // special handling for machines.
        if (spawnable is MachineItemOutput machineData && machineData.OutputMethod is { } method)
        {
            if (!StaticDelegateBuilder.TryCreateDelegate<MachineOutputDelegate>(method, out _, out string? error2))
            {
                monitor.Log($"Invalid item output method '{method}' at {breadcrumbs.Render()}:\n\n{error2}", LogLevel.Error);
            }
            return;
        }

        if (spawnable.RandomItemId is { } ids)
        {
            string[] newBreadcrumbs = [.. breadcrumbs, "RandomItemId"];
            foreach (string? candidate in ids)
            {
                this.CheckItemQuery(monitor, candidate, spawnable.PerItemCondition, [..newBreadcrumbs, candidate]);
            }
            return;
        }

        this.CheckItemQuery(monitor, spawnable.ItemId, spawnable.PerItemCondition, breadcrumbs);
    }

    private void CheckItemQuery(IMonitor monitor, string query, string? perItemCondition, string[] breadcrumbs)
    {
        ItemQueryContext context = new(Game1.currentLocation, Game1.player, Random.Shared);
        string tokenized_query = query.Replace("BOBBER_X", "4").Replace("BOBBER_Y", "6").Replace("WATER_DEPTH", "5").Replace("DROP_IN_ID", "(O)69").Replace("DROP_IN_PRESERVE", "(O)69").Replace("NEARBY_FLOWER_ID", "597").Replace("DROP_IN_QUALITY", "4");
        ItemQueryResult[] result = ItemQueryResolver.TryResolve(
            tokenized_query,
            context,
            ItemQuerySearchMode.All,
            perItemCondition,
            null,
            avoidRepeat: false,
            null,
            (string _, string queryError) => monitor.Log($"Failed parsing query '{query}' at {breadcrumbs.Render()}: {queryError}", LogLevel.Error));
        if (result.Length == 0)
        {
            monitor.Log($"Query '{query}' with condition '{perItemCondition}' did not match any items at {breadcrumbs.Render()}.", LogLevel.Warn);

            if (perItemCondition is not null)
            {
                ItemQueryResult[] candidates = ItemQueryResolver.TryResolve(
                    tokenized_query,
                    context,
                    ItemQuerySearchMode.All,
                    null,
                    null,
                    avoidRepeat: false,
                    null,
                    (string _, string queryError) => monitor.Log($"Failed parsing query '{query}' at {breadcrumbs.Render()}: {queryError}", LogLevel.Error));

                monitor.Log($"Without filtering, {candidates.Length} candidates found", LogLevel.Info);

                foreach (ItemQueryResult item in candidates)
                {
                    if (item.Item is not Item actual)
                    {
                        continue;
                    }

                    monitor.Log($"Checking {actual.QualifiedItemId} against {perItemCondition} yields {GameStateQuery.CheckConditions(perItemCondition, null, null, actual)}", LogLevel.Debug);
                }
            }

            return;
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
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1400:Access modifier should be declared", Justification = "file is a valid access modifier.")]
file static class Extensions
{
    internal static string Render(this string[] breadcrumbs) => string.Join("->", breadcrumbs);

    internal static bool IsPossibleGSQString(this string name)
        => name.EndsWith("Condition");
}
