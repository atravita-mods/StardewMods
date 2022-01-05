using System.Text;

namespace SpecialOrdersExtended.DataModels;

internal class DialogueLog : AbstractDataModel
{
    private const string identifier = "_dialogue";

    public Dictionary<string, List<string>> SeenDialogues { get; set; } = new();

    public DialogueLog(string savefile)
    {
        this.Savefile = savefile;
    }

    public static DialogueLog Load()
    {
        if (!Context.IsWorldReady) { throw new SaveNotLoadedError(); }
        return ModEntry.DataHelper.ReadGlobalData<DialogueLog>(Constants.SaveFolderName + identifier)
            ?? new DialogueLog(Constants.SaveFolderName);
    }
    public void Save()
    {
        base.Save(identifier);
    }

    public bool Contains(string dialoguekey, string characterName)
    {
        if (SeenDialogues.TryGetValue(dialoguekey, out var characterList))
        {
            return characterList.Contains(characterName);
        }
        return false;
    }

    public bool TryAdd(string dialoguekey, string characterName)
    {
        if (!SeenDialogues.TryGetValue(dialoguekey, out var characterList))
        {
            characterList = new();
            characterList.Add(characterName);
            SeenDialogues[dialoguekey] = characterList;
            return true;
        }
        else if (characterList.Contains(characterName)) { return false; }
        else { characterList.Add(characterName); return true; }
    }

    public bool TryRemove(string dialoguekey, string characterName)
    {
        if (SeenDialogues.TryGetValue(dialoguekey, out var characterList))
        {
            return characterList.Remove(characterName);
        }
        return false;
    }

    public override string ToString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append($"DialogueLog({Savefile}):");
        foreach (string key in Utilities.ContextSort(SeenDialogues.Keys))
        {
            stringBuilder.AppendLine().Append($"    {key}:").AppendJoin(", ", SeenDialogues[key]);
        }
        return stringBuilder.ToString();
    }
}
