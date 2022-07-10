using System.Reflection;
using AtraBase.Caching;
using AtraBase.Toolkit.Reflection;

namespace AtraCore.Framework.ReflectionManager;

/// <summary>
/// A class for cached reflection.
/// </summary>
public static class ReflectionCache
{
    /// <summary>
    /// Convenience flags for BindingFlags.
    /// </summary>
    public enum FlagTypes
    {
        /// <summary>
        /// For instance members, flattens hierarchy.
        /// </summary>
        InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy,

        /// <summary>
        /// For static members, flattens hierarchy.
        /// </summary>
        StaticFlags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy,

        /// <summary>
        /// For instance members, do not flatten hierarchy.
        /// </summary>
        UnflattenedInstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,

        /// <summary>
        /// For static members, does not flatten hierarchy.
        /// </summary>
        UnflattenedStaticFlags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
    }

    /// <summary>
    /// An entry for the reflection cache.
    /// </summary>
    /// <param name="Type"></param>
    /// <param name="Name"></param>
    /// <param name="FlagTypes"></param>
    /// <param name="MemberType"></param>
    /// <param name="Params"></param>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "This is a record lol.")]
    private readonly record struct ReflectionCacheMember(Type Type, string Name, FlagTypes FlagTypes, MemberTypes MemberType, Type[]? Params);

    private static readonly SimpleConcurrentCache<ReflectionCacheMember, MemberInfo> Cache = new();

    public static ConstructorInfo GetCachedConstructor(this Type type, FlagTypes flags)
        => type.GetCachedConstructor(flags, Type.EmptyTypes);

    public static ConstructorInfo GetCachedConstructor(this Type type, FlagTypes flags, Type[] paramsList)
    {
        ReflectionCacheMember cachekey = new(type, "ctor", flags, MemberTypes.Constructor, paramsList);
        if (Cache.TryGetValue(cachekey, out MemberInfo? cached) && cached is ConstructorInfo constructorInfo)
        {
            return constructorInfo;
        }
        ConstructorInfo? constructor = type.GetConstructor((BindingFlags)flags, null, paramsList, null);
        if (constructor is not null)
        {
            Cache[cachekey] = constructor;
            return constructor;
        }
        return ReflectionThrowHelper.ThrowMethodNotFoundException<ConstructorInfo>(type.FullName + "::.ctor" + string.Join(',', paramsList.Select((a) => a.FullName)));
    }

    public static MethodInfo GetCachedMethod(this Type type, string methodName, FlagTypes flags)
    {
        ReflectionCacheMember cachekey = new(type, methodName, flags, MemberTypes.Method, null);
        if (Cache.TryGetValue(cachekey, out MemberInfo? cached) && cached is MethodInfo methodInfo)
        {
            return methodInfo;
        }
        MethodInfo? method = type.GetMethod(methodName, (BindingFlags)flags);
        if (method is not null)
        {
            Cache[cachekey] = method;
            return method;
        }
        return ReflectionThrowHelper.ThrowMethodNotFoundException<MethodInfo>(type.FullName + "::" + methodName);
    }

    public static MethodInfo GetCachedMethod(this Type type, string methodName, FlagTypes flags, Type[] paramsList)
    {
        ReflectionCacheMember cachekey = new(type, methodName, flags, MemberTypes.Method, paramsList);
        if (Cache.TryGetValue(cachekey, out MemberInfo? cached) && cached is MethodInfo methodInfo)
        {
            return methodInfo;
        }
        MethodInfo? method = type.GetMethod(methodName, (BindingFlags)flags, null, paramsList, null);
        if (method is not null)
        {
            Cache[cachekey] = method;
            return method;
        }
        return ReflectionThrowHelper.ThrowMethodNotFoundException<MethodInfo>(type.FullName + "::" + methodName + string.Join(',', paramsList.Select((a) => a.FullName)));
    }

    public static PropertyInfo GetCachedProperty(this Type type, string propertyName, FlagTypes flags)
    {
        ReflectionCacheMember cachekey = new(type, propertyName, flags, MemberTypes.Property, Type.EmptyTypes);
        if (Cache.TryGetValue(cachekey, out MemberInfo? cached) && cached is PropertyInfo propertyInfo)
        {
            return propertyInfo;
        }
        PropertyInfo? property = type.GetProperty(propertyName, (BindingFlags)flags);
        if (property is not null)
        {
            Cache[cachekey] = property;
            return property;
        }
        return ReflectionThrowHelper.ThrowMethodNotFoundException<PropertyInfo>(type.FullName + "::" + propertyName);
    }

    public static FieldInfo GetCachedField(this Type type, string fieldName, FlagTypes flags)
    {
        ReflectionCacheMember cachekey = new(type, fieldName, flags, MemberTypes.Field, Type.EmptyTypes);
        if (Cache.TryGetValue(cachekey, out MemberInfo? cached) && cached is FieldInfo fieldInfo)
        {
            return fieldInfo;
        }
        FieldInfo? field = type.GetField(fieldName, (BindingFlags)flags);
        if (field is not null)
        {
            Cache[cachekey] = field;
            return field;
        }
        return ReflectionThrowHelper.ThrowMethodNotFoundException<FieldInfo>(type.FullName + "::" + fieldName);
    }

    /// <summary>
    /// Clears the reflection cache.
    /// </summary>
    internal static void Clear() => Cache.Clear();

    internal static void Swap() => Cache.Swap();
}
