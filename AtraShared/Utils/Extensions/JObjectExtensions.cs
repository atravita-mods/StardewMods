using Newtonsoft.Json.Linq;

namespace AtraShared.Utils.Extensions;

/// <summary>
/// Extensions for <see cref="JObject"/>.
/// </summary>
public static class JObjectExtensions
{
    /// <summary>
    /// Tries to get a property, ignoring case.
    /// </summary>
    /// <typeparam name="T">Type to cast to.</typeparam>
    /// <param name="obj">Object to get from.</param>
    /// <param name="key">Key to use.</param>
    /// <param name="value">Value, if possible.</param>
    /// <returns>True if valid and there, false otherwise.</returns>
    public static bool TryGetValueIgnoreCase<T>(this JObject obj, string key, out T? value)
    {
        try
        {
            if (obj.GetValue(key, StringComparison.OrdinalIgnoreCase) is { } token)
            {
                value = token.Value<T>();
                return true;
            }
        }
        catch (Exception ex)
        {
            AtraBase.Internal.Logger.Instance.Error($"[AtraShared] Failed while converting {obj} to {typeof(T).FullName}");
            AtraBase.Internal.Logger.Instance.Info(ex.ToString());
        }

        value = default;
        return false;
    }
}
