namespace SpecialOrdersExtended.DataModels;

internal abstract class AbstractDataModel
{
    public string Savefile { get; set; }

    public virtual void Save(string identifier)
    {
        ModEntry.DataHelper.WriteGlobalData(Savefile + identifier, this);
    }

}
