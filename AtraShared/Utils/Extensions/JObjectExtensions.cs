using Newtonsoft.Json.Linq;

namespace AtraShared.Utils.Extensions;

public static class JObjectExtensions
{
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
