using System.Runtime.CompilerServices;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MiniAtraShared;

/// <summary>
/// Utility methods.
/// </summary>
public static class Utils
{
    /// <summary>
    /// A Lazy that contains a single pixel, useful for drawing geometric shapes.
    /// </summary>
    /// <remarks>Taken from https://github.com/Pathoschild/StardewMods/blob/develop/Common/CommonHelper.cs . Much thanks.</remarks>
    private static readonly Lazy<Texture2D> LazyPixel = new (() =>
    {
        Texture2D pixel = new (Game1.graphics.GraphicsDevice, 1, 1);
        pixel.SetData([Color.White]);
        return pixel;
    });

    /// <summary>
    /// Gets a Pixel that can be used for drawing arbitrary geometric shapes.
    /// </summary>
    public static Texture2D Pixel => LazyPixel.Value;

    /// <summary>
    /// Gets the configuration instance, or returns a default one.
    /// </summary>
    /// <typeparam name="T">Type of config.</typeparam>
    /// <param name="helper">Smapi's helper.</param>
    /// <param name="monitor">Logger.</param>
    /// <returns>Config.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T GetConfigOrDefault<T>(IModHelper helper, IMonitor monitor)
        where T : class, new()
    {
        try
        {
            return helper.ReadConfig<T>();
        }
        catch (Exception ex)
        {
            monitor.Log(
                helper.Translation.Get("IllFormatedConfig")
                    .Default("Config file seems ill-formated, using default. Please use Generic Mod Config Menu to configure."),
                LogLevel.Warn);
            monitor.Log(ex.ToString());
            return new ();
        }
    }
}