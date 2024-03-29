﻿using AtraBase.Toolkit;

using StardewModdingAPI.Utilities;

namespace SpecialOrdersExtended.DataModels;

/// <summary>
/// Base data model class.
/// </summary>
public abstract class AbstractDataModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractDataModel"/> class.
    /// </summary>
    /// <param name="savefile">String that represents the savefile name.</param>
    /// <remarks>SaveFile name is farmname + unique ID in 1.5+.</remarks>
    public AbstractDataModel(string savefile) => this.SaveFile = savefile;

    /// <summary>
    /// Gets or sets string that represents the savefile name.
    /// </summary>
    /// <remarks>SaveFile name is unique ID in 1.5+.</remarks>
    public virtual string SaveFile { get; set; }

    /// <summary>
    /// Handles saving.
    /// </summary>
    /// <param name="identifier">An identifier token to add to the filename.</param>
    internal virtual void Save(string identifier)
    {
        Task.Run(() => ModEntry.DataHelper.WriteGlobalData(this.SaveFile.GetStableHashCode() + identifier, this))
            .ContinueWith((t) => ModEntry.ModMonitor.Log(t.Status == TaskStatus.RanToCompletion ? $"Saved {identifier}" : $"{identifier} failed to save with {t.Status} - {t.Exception}"));
    }

    /// <summary>
    /// A way to save a temporary file.
    /// </summary>
    /// <param name="identifier">An identifier token to add to the filename.</param>
    /// <remarks>NOT IMPLEMENTED YET.</remarks>
    internal virtual void SaveTemp(string identifier) => this.Save($"{identifier}_temp_{SDate.Now().DaysSinceStart}");
}
