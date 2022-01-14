using StardewModdingAPI.Utilities;

namespace SpecialOrdersExtended.DataModels;

internal abstract class AbstractDataModel
{
    public virtual string Savefile { get; set; }

    public AbstractDataModel(string savefile) => this.Savefile = savefile;

    public virtual void Save(string identifier) => ModEntry.DataHelper.WriteGlobalData(this.Savefile + identifier, this);

    public virtual void SaveTemp(string identifier) => this.Save($"{identifier}_temp_{SDate.Now().DaysSinceStart}");

}
