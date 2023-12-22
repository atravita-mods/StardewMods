using System.Reflection;

namespace SinZsEventTester.Framework;

/// <summary>
/// Tests GSQ.
/// </summary>
/// <param name="monitor">The monitor instance to use.</param>
/// <param name="reflector">SMAPI's reflection helper.</param>
internal sealed class GSQTester(IMonitor monitor, IReflectionHelper reflector)
{
    private readonly SObject puffer = new("420", 1);

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

                string[] breadcrumbs = [$"Data/{method.Name.Replace('_', '/')}"];

                this.Process(data, breadcrumbs);
            }
        }
    }

    private void Process(object data, string[] breadcrumbs)
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
                processor.Invoke(this, [data, breadcrumbs]);
            }
            else if (t.GetGenericTypeDefinition() == typeof(List<>))
            {
                MethodInfo processor = this.GetType().GetMethod(nameof(this.ProcessList), BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(dataType);
                processor.Invoke(this, [data, breadcrumbs]);
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
                if (field.Name.IsPossibleGSQString() && field.FieldType == typeof(string))
                {
                    string? gsq = (string?)field.GetValue(data);
                    this.CheckGSQ(gsq, [..breadcrumbs, field.Name]);
                }
                else if (!field.FieldType.IsValueType && (field.FieldType.IsGenericType || field.FieldType.Assembly!.GetName()!.Name!.Contains("StardewValley", StringComparison.OrdinalIgnoreCase)))
                {
                    object f = field.GetValue(data)!;
                    this.Process(f, [.. breadcrumbs, field.Name]);
                }
            }

            foreach (PropertyInfo prop in t.GetProperties())
            {
                if (prop.Name.IsPossibleGSQString() && prop.PropertyType == typeof(string))
                {
                    string? gsq = (string?)prop.GetValue(data);
                    this.CheckGSQ(gsq, [..breadcrumbs, prop.Name]);
                }
                else if (!prop.PropertyType.IsValueType && (prop.PropertyType.IsGenericType || prop.PropertyType.Assembly!.GetName()!.Name!.Contains("StardewValley", StringComparison.OrdinalIgnoreCase)))
                {
                    object f = prop.GetValue(data)!;
                    this.Process(f, [.. breadcrumbs, prop.Name]);
                }
            }
        }
    }

    private void ProcessDictionary<TKey, TValue>(Dictionary<TKey, TValue> data, string[] breadcrumbs)
        where TKey : notnull
    {
        foreach ((TKey k, TValue v) in data)
        {
            this.Process(v!, [.. breadcrumbs, k.ToString()!]);
        }
    }

    private void ProcessList<T>(List<T> data, string[] breadcrumbs)
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

            this.Process(v!, [.. breadcrumbs, id]);
        }
    }

    private void CheckGSQ(string? gsq, string[] breadcrumbs)
    {
        if (string.IsNullOrEmpty(gsq))
        {
            return;
        }

        monitor.Log($"Checking: {gsq}\n{breadcrumbs.Render()}", LogLevel.Info);
        GameStateQuery.CheckConditions(gsq, null, null, this.puffer, this.puffer, Random.Shared);
    }
}

file static class Extensions
{
    internal static string Render(this string[] breadcrumbs) => string.Join("->", breadcrumbs);

    internal static bool IsPossibleGSQString(this string name)
    {
        if (name.EndsWith("Condition"))
        {
            return true;
        }

        return name is "CanSocialize" or "CanVisitIsland" or "ItemDeliveryQuest" or "WinterStarParticipant" or "MinecartsUnlocked";
    }
}
