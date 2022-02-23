using NotNullAttribute = System.Diagnostics.CodeAnalysis.NotNullAttribute;

namespace AtraShared.Integrations;

internal class IntegrationHelper
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationHelper"/> class.
    /// </summary>
    /// <param name="monitor"></param>
    /// <param name="translation">Translation helper.</param>
    /// <param name="modRegistry">Mod registery.</param>
    public IntegrationHelper(IMonitor monitor, ITranslationHelper translation, IModRegistry modRegistry)
    {
        this.Monitor = monitor;
        this.Translation = translation;
        this.ModRegistry = modRegistry;
    }

    public IMonitor Monitor { get; private set; }

    public ITranslationHelper Translation { get; private set; }

    public IModRegistry ModRegistry { get; private set; }

    public bool TryGetAPI<T>(
        [NotNull] string apiid,
        [NotNull] string minversion,
        [NotNullWhen(returnValue: true)] out T? api)
        where T : class?
    {
        if (this.ModRegistry.Get(apiid) is not IModInfo modInfo)
        {
            this.Monitor.Log(
                this.Translation.Get("api-not-found")
                .Default("{{APIID}} not found, integration disabled.")
                .Tokens(new { apiid }), LogLevel.Info);
            api = default;
            return false;
        }
        if (modInfo.Manifest.Version.IsOlderThan(minversion))
        {
            this.Monitor.Log(
                this.Translation.Get("api-too-old")
                .Default("Please update {{APIID}} to at least version {{minversion}}. Current version {{currentversion}}. Integration disabled")
                .Tokens(new { apiid, minversion, currentversion = modInfo.Manifest.Version }), LogLevel.Info);
            api = default;
            return false;
        }
        api = this.ModRegistry.GetApi<T>(apiid);
        return api is not null;
    }
}