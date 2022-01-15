using StardewModdingAPI.Utilities;

namespace SpecialOrdersExtended.DataModels;

/// <summary>
/// Base data model class
/// </summary>
internal abstract class AbstractDataModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractDataModel"/> class.
    /// </summary>
    /// <param name="savefile">String that represents the savefile name.</param>
    /// <remarks>Savefile name is farmname + unique ID in 1.5+.</remarks>
    public AbstractDataModel(string savefile) => this.Savefile = savefile;

    /// <summary>
    /// Gets or sets string that represents the savefile name.
    /// </summary>
    /// <remarks>Savefile name is farmname + unique ID in 1.5+.</remarks>
    public virtual string Savefile { get; set; }

    public virtual void Save(string identifier) => ModEntry.DataHelper.WriteGlobalData(this.Savefile + identifier, this);

    public virtual void SaveTemp(string identifier) => this.Save($"{identifier}_temp_{SDate.Now().DaysSinceStart}");

}
