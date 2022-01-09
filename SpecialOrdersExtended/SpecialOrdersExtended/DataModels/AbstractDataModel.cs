using StardewModdingAPI.Utilities;

namespace SpecialOrdersExtended.DataModels;

internal abstract class AbstractDataModel
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public virtual string Savefile { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public virtual void Save(string identifier)
    {
        ModEntry.DataHelper.WriteGlobalData(this.Savefile + identifier, this);
    }

    public virtual void SaveTemp(string identifier)
    {
        this.Save($"{identifier}_temp_{SDate.Now().DaysSinceStart}");
    }

}
