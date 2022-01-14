using StardewModdingAPI.Utilities;
using System.Text;

namespace SpecialOrdersExtended.DataModels;

internal class DialogueLog : AbstractDataModel
{
    private const string identifier = "_dialogue";

    public Dictionary<string, List<string>> SeenDialogues { get; set; } = new();

    public DialogueLog(string savefile) : base(savefile)
    {
    }

    public static DialogueLog Load()
    {
        if (!Context.IsWorldReady) { throw new SaveNotLoadedError(); }
        return ModEntry.DataHelper.ReadGlobalData<DialogueLog>(Constants.SaveFolderName + identifier)
            ?? new DialogueLog(Constants.SaveFolderName);
    }

    public static DialogueLog LoadTempIfAvailable()
    {
        throw new NotImplementedException();
    }

    public void SaveTemp() => base.SaveTemp(identifier);
    public void Save() => base.Save(identifier);

    [Pure]
    public bool Contains(string dialoguekey, string characterName)
    {
        if (this.SeenDialogues.TryGetValue(dialoguekey, out List<string>? characterList))
        {
            return characterList.Contains(characterName);
        }
        return false;
    }

    public bool TryAdd(string dialoguekey, string characterName)
    {
        if (!this.SeenDialogues.TryGetValue(dialoguekey, out List<string>? characterList))
        {
            characterList = new();
            characterList.Add(characterName);
            this.SeenDialogues[dialoguekey] = characterList;
            return true;
        }
        else if (characterList.Contains(characterName)) { return false; }
        else { characterList.Add(characterName); return true; }
    }

    public bool TryRemove(string dialoguekey, string characterName)
    {
        if (this.SeenDialogues.TryGetValue(dialoguekey, out List<string>? characterList))
        {
            return characterList.Remove(characterName);
        }
        return false;
    }

    [Pure]
    public override string ToString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append($"DialogueLog({this.Savefile}):");
        foreach (string key in Utilities.ContextSort(this.SeenDialogues.Keys))
        {
            stringBuilder.AppendLine().Append($"    {key}:").AppendJoin(", ", this.SeenDialogues[key]);
        }
        return stringBuilder.ToString();
    }
}
