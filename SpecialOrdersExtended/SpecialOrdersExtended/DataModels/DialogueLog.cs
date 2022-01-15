using StardewModdingAPI.Utilities;
using System.Text;

namespace SpecialOrdersExtended.DataModels;

internal class DialogueLog : AbstractDataModel
{
    private const string identifier = "_dialogue";
    private readonly long multiplayerID;

    public DialogueLog(string savefile, long multiplayerID)
    : base(savefile)
    {
        this.multiplayerID = multiplayerID;
    }

    public Dictionary<string, List<string>> SeenDialogues { get; set; } = new();

    /// <summary>
    /// Loads a dialogueLog.
    /// </summary>
    /// <param name="multiplayerID">Multiplayer ID of the player who requested it.</param>
    /// <returns>The dialogueLog in question.</returns>
    /// <exception cref="SaveNotLoadedError"></exception>
    public static DialogueLog Load(long multiplayerID)
    {
        if (!Context.IsWorldReady) { throw new SaveNotLoadedError(); }
        return ModEntry.DataHelper.ReadGlobalData<DialogueLog>($"{Constants.SaveFolderName}{identifier}{multiplayerID}")
            ?? new DialogueLog(Constants.SaveFolderName, multiplayerID);
    }

    public static DialogueLog LoadTempIfAvailable(long multiplayerID)
    {
        throw new NotImplementedException();
    }

    public void SaveTemp() => base.SaveTemp(identifier + this.multiplayerID.ToString());

    public void Save() => base.Save(identifier + this.multiplayerID.ToString());

    [Pure]
    public bool Contains(string dialoguekey, string characterName)
    {
        if (this.SeenDialogues.TryGetValue(dialoguekey, out List<string>? characterList))
        {
            return characterList.Contains(characterName);
        }
        return false;
    }

    /// <summary>
    /// Tries to add a dialogue key to a character's SeenDialogues, if they haven't seen it before
    /// </summary>
    /// <param name="dialoguekey"></param>
    /// <param name="characterName"></param>
    /// <returns>True if successfully added, false otherwise</returns>
    public bool TryAdd(string dialoguekey, string characterName)
    {
        if (!this.SeenDialogues.TryGetValue(dialoguekey, out List<string>? characterList))
        {
            characterList = new();
            characterList.Add(characterName);
            this.SeenDialogues[dialoguekey] = characterList;
            return true;
        }
        else if (characterList.Contains(characterName))
        {
            return false;
        }
        else
        {
            characterList.Add(characterName);
            return true;
        }
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
