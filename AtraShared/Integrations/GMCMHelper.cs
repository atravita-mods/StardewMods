using System.Reflection;
using AtraShared.Utils.Extensions;
using StardewModdingAPI.Utilities;

namespace AtraShared.Integrations;

/// <summary>
/// Helper class that generates the GMCM for a project.
/// </summary>
internal class GMCMHelper : IntegrationHelper
{
    private const string MINVERSION = "1.8.0";
    private const string APIID = "spacechase0.GenericModConfigMenu";

    private readonly IManifest manifest;

    private IGenericModConfigMenuApi? modMenuApi;

    /// <summary>
    /// Initializes a new instance of the <see cref="GMCMHelper"/> class.
    /// </summary>
    /// <param name="monitor">Logger.</param>
    /// <param name="translation">Translation helper.</param>
    /// <param name="modRegistry">Mod registry helper.</param>
    /// <param name="manifest">Mod's manifest.</param>
    public GMCMHelper(IMonitor monitor, ITranslationHelper translation, IModRegistry modRegistry, IManifest manifest)
        : base(monitor, translation, modRegistry)
    {
        this.manifest = manifest;
    }

    /// <summary>
    /// Tries to grab a copy of the API.
    /// </summary>
    /// <returns>True if successful, false otherwise.</returns>
    public bool TryGetAPI() => this.TryGetAPI(APIID, MINVERSION, out this.modMenuApi);

    /// <summary>
    /// Register mod with GMCM.
    /// </summary>
    /// <param name="reset">Reset callback.</param>
    /// <param name="save">Save callback.</param>
    /// <param name="titleScreenOnly">Whether or not the config should only be availble from the title screen.</param>
    /// <returns>this.</returns>
    public GMCMHelper Register(Action reset, Action save, bool titleScreenOnly = false)
    {
        this.modMenuApi!.Register(
            mod: this.manifest,
            reset: reset,
            save: save,
            titleScreenOnly: titleScreenOnly);
        return this;
    }

    /// <summary>
    /// Adds some text at this location on the form.
    /// </summary>
    /// <param name="paragraph">Delegate to get the text.</param>
    /// <returns>this.</returns>
    public GMCMHelper AddParagraph(Func<string> paragraph)
    {
        this.modMenuApi!.AddParagraph(
            mod: this.manifest,
            text: paragraph);
        return this;
    }

    /// <summary>
    /// Adds some text at this location on the form, using the given translation key.
    /// </summary>
    /// <param name="translationKey">Translation key to use.</param>
    /// <returns>this.</returns>
    public GMCMHelper AddParagraph(string translationKey)
    {
        this.AddParagraph(() => this.Translation.Get(translationKey));
        return this;
    }

    /// <summary>
    /// Adds some text at this location on the form, using the given translation key and tokens.
    /// </summary>
    /// <param name="translationKey">translation key.</param>
    /// <param name="tokens">tokens for translation.</param>
    /// <returns>this.</returns>
    public GMCMHelper AddParagraph(string translationKey, object tokens)
    {
        this.AddParagraph(() => this.Translation.Get(translationKey, tokens));
        return this;
    }

    /// <summary>
    /// Adds a boolean option at a specific location.
    /// </summary>
    /// <param name="name">Function to get the name of the option.</param>
    /// <param name="getValue">Getvalue callback.</param>
    /// <param name="setValue">Setvalue callback.</param>
    /// <param name="tooltip">Function to get the tooltip for the option.</param>
    /// <param name="fieldId">FieldID.</param>
    /// <returns>this.</returns>
    public GMCMHelper AddBoolOption(
        Func<string> name,
        Func<bool> getValue,
        Action<bool> setValue,
        Func<string>? tooltip = null,
        string? fieldId = null)
    {
        this.modMenuApi!.AddBoolOption(
            mod: this.manifest,
            name: name,
            getValue: getValue,
            setValue: setValue,
            tooltip: tooltip,
            fieldId: fieldId);
        return this;
    }

    /// <summary>
    /// Adds a boolean option at a specific location.
    /// </summary>
    /// <typeparam name="T">Type of the ModConfig.</typeparam>
    /// <param name="property">Property to process.</param>
    /// <param name="getConfig">Function that gets the *current config instance*.</param>
    /// <param name="fieldId">FieldId.</param>
    /// <returns>this.</returns>
    public GMCMHelper AddBoolOption<T>(
        PropertyInfo property,
        Func<T> getConfig,
        string? fieldId = null)
    {
        if (property.GetGetMethod() is not MethodInfo getter || property.GetSetMethod() is not MethodInfo setter)
        {
            this.Monitor.DebugLog($"{property.Name} appears to be a misconfigured option!", LogLevel.Warn);
        }
        else
        {
            Func<T, bool> getterDelegate = getter.CreateDelegate<Func<T, bool>>();
            Action<T, bool>? setterDelegate = setter.CreateDelegate<Action<T, bool>>();
            this.AddBoolOption(
                name: () => this.Translation.Get($"{property.Name}.title"),
                tooltip: () => this.Translation.Get($"{property.Name}.description"),
                getValue: () => getterDelegate(getConfig()),
                setValue: value => setterDelegate(getConfig(), value),
                fieldId: fieldId);
        }
        return this;
    }

    /// <summary>
    /// Adds a text option at the given location.
    /// </summary>
    /// <param name="name">Function to get the name of the option.</param>
    /// <param name="getValue">Getvalue callback.</param>
    /// <param name="setValue">Setvalue callback.</param>
    /// <param name="tooltip">Function to get the tooltip of this option.</param>
    /// <param name="allowedValues">Array of allowed values.</param>
    /// <param name="formatAllowedValue">Format map for allowed values.</param>
    /// <param name="fieldId">FieldID.</param>
    /// <returns>this.</returns>
    public GMCMHelper AddTextOption(
        Func<string> name,
        Func<string> getValue,
        Action<string> setValue,
        Func<string>? tooltip = null,
        string[]? allowedValues = null,
        Func<string, string>? formatAllowedValue = null,
        string? fieldId = null)
    {
        this.modMenuApi!.AddTextOption(
            mod: this.manifest,
            name: name,
            getValue: getValue,
            setValue: setValue,
            tooltip: tooltip,
            allowedValues: allowedValues,
            formatAllowedValue: formatAllowedValue,
            fieldId: fieldId);
        return this;
    }

    /// <summary>
    /// Adds a text option at the given location.
    /// </summary>
    /// <typeparam name="T">ModConfig's type.</typeparam>
    /// <param name="property">Property to process.</param>
    /// <param name="getConfig">Function that gets the *current config instance*</param>
    /// <param name="allowedValues">Allowed values.</param>
    /// <param name="formatAllowedValue">Formatter.</param>
    /// <param name="fieldId">fieldId.</param>
    /// <returns>this.</returns>
    public GMCMHelper AddTextOption<T>(
        PropertyInfo property,
        Func<T> getConfig,
        string[]? allowedValues = null,
        Func<string, string>? formatAllowedValue = null,
        string? fieldId = null)
    {
        if (property.GetGetMethod() is not MethodInfo getter || property.GetSetMethod() is not MethodInfo setter)
        {
            this.Monitor.DebugLog($"{property.Name} appears to be a misconfigured option!", LogLevel.Warn);
        }
        else
        {
            Func<T, string> getterDelegate = getter.CreateDelegate<Func<T, string>>();
            Action<T, string>? setterDelegate = setter.CreateDelegate<Action<T, string>>();
            this.AddTextOption(
                name: () => this.Translation.Get($"{property.Name}.title"),
                tooltip: () => this.Translation.Get($"{property.Name}.description"),
                getValue: () => getterDelegate(getConfig()),
                setValue: value => setterDelegate(getConfig(), value),
                allowedValues: allowedValues,
                formatAllowedValue: formatAllowedValue,
                fieldId: fieldId);
        }
        return this;
    }

    /// <summary>
    /// Adds a enum option at the given location.
    /// </summary>
    /// <typeparam name="TEnum">Type of the enum.</typeparam>
    /// <param name="name">Function to get the name of the option.</param>
    /// <param name="getValue">GetValue callback.</param>
    /// <param name="setValue">SetValue callback.</param>
    /// <param name="tooltip">Function to get the tooltip of the option.</param>
    /// <param name="fieldId">FieldID.</param>
    /// <returns>this.</returns>
    public GMCMHelper AddEnumOption<TEnum>(
        Func<string> name,
        Func<string> getValue,
        Action<string> setValue,
        Func<string>? tooltip = null,
        string? fieldId = null)
        where TEnum : struct, Enum
    {
        this.AddTextOption(
            name: name,
            getValue: getValue,
            setValue: setValue,
            tooltip: tooltip,
            allowedValues: Enum.GetNames<TEnum>(),
            formatAllowedValue: value => this.Translation.Get($"config.{typeof(TEnum).Name}.{value}"),
            fieldId: fieldId);
        return this;
    }

    /// <summary>
    /// Adds an enum option at the given location.
    /// </summary>
    /// <typeparam name="TEnum">Type of the enum.</typeparam>
    /// <param name="name">Name of the field.</param>
    /// <param name="getValue">Getvalue callback.</param>
    /// <param name="setValue">Setvalue callback.</param>
    /// <param name="tooltip">Function to get the tooltip.</param>
    /// <param name="fieldId">FieldId.</param>
    /// <returns>this.</returns>
    public GMCMHelper AddEnumOption<TEnum>(
        Func<string> name,
        Func<TEnum> getValue,
        Action<TEnum> setValue,
        Func<string>? tooltip = null,
        string? fieldId = null)
        where TEnum : struct, Enum
    {
        this.AddEnumOption<TEnum>(
            name: name,
            getValue: getValue().ToString,
            setValue: (value) => setValue(Enum.Parse<TEnum>(value)),
            tooltip: tooltip,
            fieldId: fieldId);
        return this;
    }

    /// <summary>
    /// Adds a float option at this point in the form.
    /// </summary>
    /// <param name="name">Function to get the name of the option.</param>
    /// <param name="getValue">GetValue callback.</param>
    /// <param name="setValue">SetValue callback.</param>
    /// <param name="tooltip">Tooltip callback.</param>
    /// <param name="min">Minimum value.</param>
    /// <param name="max">Maximum value.</param>
    /// <param name="interval">Itnerval. </param>
    /// <param name="formatValue">Format function.</param>
    /// <param name="fieldId">FieldId.</param>
    /// <returns>this.</returns>
    public GMCMHelper AddNumberOption(
        Func<string> name,
        Func<float> getValue,
        Action<float> setValue,
        Func<string>? tooltip = null,
        float? min = null,
        float? max = null,
        float? interval = null,
        Func<float, string>? formatValue = null,
        string? fieldId = null)
    {
        this.modMenuApi!.AddNumberOption(
            mod: this.manifest,
            name: name,
            getValue: getValue,
            setValue: setValue,
            tooltip: tooltip,
            min: min,
            max: max,
            interval: interval,
            formatValue: formatValue,
            fieldId: fieldId);
        return this;
    }

    /// <summary>
    /// Adds a float option at this point in the form.
    /// </summary>
    /// <typeparam name="T">ModConfig's type.</typeparam>
    /// <param name="property">Property to process.</param>
    /// <param name="getConfig">Function that gets the current config instance.</param>
    /// <param name="min">Min.</param>
    /// <param name="max">Max.</param>
    /// <param name="interval">Interval.</param>
    /// <param name="formatValue">Formmater.</param>
    /// <param name="fieldID">fieldId.</param>
    /// <returns>this.</returns>
    public GMCMHelper AddFloatOption<T>(
        PropertyInfo property,
        Func<T> getConfig,
        float? min = null,
        float? max = null,
        float? interval = null,
        Func<float, string>? formatValue = null,
        string? fieldID = null)
    {
        if (property.GetGetMethod() is not MethodInfo getter || property.GetSetMethod() is not MethodInfo setter)
        {
            this.Monitor.DebugLog($"{property.Name} appears to be a misconfigured option!", LogLevel.Warn);
        }
        else
        {
            Func<T, float> getterDelegate = getter.CreateDelegate<Func<T, float>>();
            Action<T, float>? setterDelegate = setter.CreateDelegate<Action<T, float>>();
            this.AddNumberOption(
                name: () => this.Translation.Get($"{property.Name}.title"),
                tooltip: () => this.Translation.Get($"{property.Name}.description"),
                getValue: () => getterDelegate(getConfig()),
                setValue: value => setterDelegate(getConfig(), value),
                min: min,
                max: max,
                interval: interval,
                formatValue: formatValue,
                fieldId: fieldID);
        }
        return this;
    }

    /// <summary>
    /// Adds an int option at this point in the form.
    /// </summary>
    /// <param name="name">Function to get the name of the option.</param>
    /// <param name="getValue">GetValue callback.</param>
    /// <param name="setValue">SetValue callback.</param>
    /// <param name="tooltip">Tooltip callback.</param>
    /// <param name="min">Minimum value.</param>
    /// <param name="max">Maximum value.</param>
    /// <param name="interval">Interval. </param>
    /// <param name="formatValue">Format function.</param>
    /// <param name="fieldId">FieldId.</param>
    /// <returns>this.</returns>
    public GMCMHelper AddNumberOption(
        Func<string> name,
        Func<int> getValue,
        Action<int> setValue,
        Func<string>? tooltip = null,
        int? min = null,
        int? max = null,
        int? interval = null,
        Func<int, string>? formatValue = null,
        string? fieldId = null)
    {
        this.modMenuApi!.AddNumberOption(
            mod: this.manifest,
            name: name,
            getValue: getValue,
            setValue: setValue,
            tooltip: tooltip,
            min: min,
            max: max,
            interval: interval,
            formatValue: formatValue,
            fieldId: fieldId);
        return this;
    }

    /// <summary>
    /// Adds an int option at this point in the form.
    /// </summary>
    /// <typeparam name="T">ModConfig's type.</typeparam>
    /// <param name="property">Property to process.</param>
    /// <param name="getConfig">Function that gets the current config instance.</param>
    /// <param name="min">Min value.</param>
    /// <param name="max">Max value.</param>
    /// <param name="interval">Interval.</param>
    /// <param name="formatValue">Formats values.</param>
    /// <param name="fieldId">fieldId.</param>
    /// <returns>this.</returns>
    public GMCMHelper AddIntOption<T>(
        PropertyInfo property,
        Func<T> getConfig,
        int? min = null,
        int? max = null,
        int? interval = null,
        Func<int, string>? formatValue = null,
        string? fieldId = null)
    {
        if (property.GetGetMethod() is not MethodInfo getter || property.GetSetMethod() is not MethodInfo setter)
        {
            this.Monitor.DebugLog($"{property.Name} appears to be a misconfigured option!", LogLevel.Warn);
        }
        else
        {
            Func<T, int> getterDelegate = getter.CreateDelegate<Func<T, int>>();
            Action<T, int>? setterDelegate = setter.CreateDelegate<Action<T, int>>();
            this.AddNumberOption(
                name: () => this.Translation.Get($"{property.Name}.title"),
                tooltip: () => this.Translation.Get($"{property.Name}.description"),
                getValue: () => getterDelegate(getConfig()),
                setValue: value => setterDelegate(getConfig(), value),
                min: min,
                max: max,
                interval: interval,
                formatValue: formatValue,
                fieldId: fieldId);
        }
        return this;
    }

    /// <summary>
    /// Adds a KeyBindList at this position in the form.
    /// </summary>
    /// <param name="name">Function to get the name.</param>
    /// <param name="getValue">GetValue callback</param>
    /// <param name="setValue">SetValue callback.</param>
    /// <param name="tooltip">Function to get the tooltip.</param>
    /// <param name="fieldId">FieldID.</param>
    /// <returns>this.</returns>
    public GMCMHelper AddKeybindList(
        Func<string> name,
        Func<KeybindList> getValue,
        Action<KeybindList> setValue,
        Func<string>? tooltip = null,
        string? fieldId = null)
    {
        this.modMenuApi!.AddKeybindList(
            mod: this.manifest,
            name: name,
            getValue: getValue,
            setValue: setValue,
            tooltip: tooltip,
            fieldId: fieldId);
        return this;
    }

    /// <summary>
    /// Adds a keybindlist option at this point in the form.
    /// </summary>
    /// <typeparam name="T">ModConfig's type.</typeparam>
    /// <param name="property">Property to process.</param>
    /// <param name="getConfig">Function that gets the current config instance.</param>
    /// <param name="fieldID">fieldId.</param>
    /// <returns>this.</returns>
    public GMCMHelper AddKeybindList<T>(
        PropertyInfo property,
        Func<T> getConfig,
        string? fieldID = null)
    {
        if (property.GetGetMethod() is not MethodInfo getter || property.GetSetMethod() is not MethodInfo setter)
        {
            this.Monitor.DebugLog($"{property.Name} appears to be a misconfigured option!", LogLevel.Warn);
        }
        else
        {
            Func<T, KeybindList> getterDelegate = getter.CreateDelegate<Func<T, KeybindList>>();
            Action<T, KeybindList>? setterDelegate = setter.CreateDelegate<Action<T, KeybindList>>();
            this.AddKeybindList(
                name: () => this.Translation.Get($"{property.Name}.title"),
                tooltip: () => this.Translation.Get($"{property.Name}.description"),
                getValue: () => getterDelegate(getConfig()),
                setValue: value => setterDelegate(getConfig(), value),
                fieldId: fieldID);
        }
        return this;
    }
}