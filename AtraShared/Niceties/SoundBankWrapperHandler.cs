using System.Reflection;
using AtraBase.Toolkit.Reflection;
using AtraCore.Framework.ReflectionManager;
using FastExpressionCompiler.LightExpression;
using Microsoft.Xna.Framework.Audio;

namespace AtraShared.Niceties;

public static class SoundBankWrapperHandler
{
    private static readonly Lazy<Func<SoundBankWrapper, SoundBank>> getActualSoundBank = new(() =>
    {
        FieldInfo? field = typeof(SoundBankWrapper).GetCachedField("soundBank", ReflectionCache.FlagTypes.InstanceFlags);
        return field.GetInstanceFieldGetter<SoundBankWrapper, SoundBank>();
    });

    /// <summary>
    /// Gets the actual soundbank from a SoundBankWrapper.
    /// </summary>
    public static Func<SoundBankWrapper, SoundBank> GetActualSoundBank => getActualSoundBank.Value;

    private static readonly Lazy<Func<SoundBank, ICollection<string>>> getCues = new(() =>
    {
        FieldInfo? field = typeof(SoundBank).GetCachedField("_cues", ReflectionCache.FlagTypes.InstanceFlags);

        // Get the _cues private field from the soundbank.
        var param = Expression.Parameter(typeof(SoundBank), "soundbank");
        var fieldgetter = Expression.Field(param, field);

        // Call the .Keys property.
        var getter = typeof(Dictionary<string, CueDefinition>).GetCachedProperty(nameof(Dictionary<string, CueDefinition>.Keys), ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod()!;
        var express = Expression.Call(fieldgetter, getter);
        return Expression.Lambda<Func<SoundBank, ICollection<string>>>(express, param).CompileFast();
    });

    /// <summary>
    /// Gets an ICollection[string] representing the loaded music cues.
    /// </summary>
    public static Func<SoundBank, ICollection<string>> GetCues => getCues.Value;

    private static readonly Lazy<Func<SoundBank, string, bool>> hasCue = new(() =>
    {
        FieldInfo? field = typeof(SoundBank).GetCachedField("_cues", ReflectionCache.FlagTypes.InstanceFlags);

        // Get the _cues private field from the soundbank.
        var param = Expression.Parameter(typeof(SoundBank), "soundbank");
        var name = Expression.Parameter(typeof(string), "name");
        var fieldgetter = Expression.Field(param, field);

        // call the ContainsKey
        var containsKey = typeof(Dictionary<string, CueDefinition>).GetCachedMethod(nameof(Dictionary<string, CueDefinition>.ContainsKey), ReflectionCache.FlagTypes.InstanceFlags);
        var express = Expression.Call(fieldgetter, containsKey, name);
        return Expression.Lambda<Func<SoundBank, string, bool>>(express, param, name).CompileFast();
    });

    /// <summary>
    /// Gets whether or not a specified string key exists in the loaded music cues.
    /// </summary>
    public static Func<SoundBank, string, bool> HasCue => hasCue.Value;
}
