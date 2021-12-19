using System;
using System.Collections.Generic;
using System.Text;
using StardewModdingAPI;

using StardewValley;

namespace SpecialOrdersExtended.DataModels
{
    internal class DialogueLog : AbstractDataModel
    {
        private new const string identifier = "_dialogue";

        public Dictionary<string, List<string>> SeenDialogues { get; set; } = new();

        public DialogueLog(string savefile)
        {
            this.Savefile = savefile;
        }

        public static DialogueLog Load()
        {
            if (!Context.IsWorldReady) { throw new SaveNotLoadedError(); }
            return ModEntry.DataHelper.ReadGlobalData<DialogueLog>(Constants.SaveFolderName + identifier) ?? new DialogueLog(Constants.SaveFolderName);
        }
        public void Save()
        {
            base.Save(identifier);
        }

        public bool Contains(string dialoguekey, string characterName)
        {
            if (SeenDialogues.TryGetValue(dialoguekey, out List<string> characterList))
            {
                return characterList.Contains(characterName);
            }
            return false;
        }

        public bool Add(string dialoguekey, string characterName)
        {
            if (!SeenDialogues.TryGetValue(dialoguekey, out List<string> characterList))
            {
                characterList = new();
                characterList.Add(characterName);
                SeenDialogues[dialoguekey] = characterList;
                return true;
            }
            if (characterList.Contains(characterName)) { return false; }
            else { characterList.Add(characterName); return true; }
        }

        public bool Remove(string dialoguekey, string characterName)
        {
            if (SeenDialogues.TryGetValue(dialoguekey, out List<string> characterList))
            {
                return characterList.Remove(characterName);
            }
            return false;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new();
            stringBuilder.Append($"DialogueLog({Savefile}):");
            foreach (string key in SeenDialogues.Keys)
            {
                stringBuilder.Append($"\n    {key}: {String.Join(", ", SeenDialogues[key])}");
            }
            return stringBuilder.ToString();
        }
    }
}
