using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI;

using StardewValley;

namespace SpecialOrdersExtended
{

    internal class DialogueManager
    {
        public static void ConsoleSpecialOrderDialogue(string command, string[] args)
        {
            if(args.Length<3)
            {
                ModEntry.ModMonitor.Log($"{command} requires at least a desired action, a key, and at least one NPC name", LogLevel.Warn);
                return;
            }
            switch (args[0])
            {
                case "add":
                    foreach (string characterName in args[2..])
                    {
                        if(TryAddSeenDialogue(args[1], characterName))
                        {
                            ModEntry.ModMonitor.Log($"{args[1]}: added {characterName}", LogLevel.Info);
                        }
                        else
                        {
                            ModEntry.ModMonitor.Log($"{args[1]}: could not add {characterName}; already in list", LogLevel.Info);
                        }
                    }
                    break;
                case "remove":
                    foreach (string characterName in args[2..])
                    {
                        if(TryRemoveSeenDialogue(args[1], characterName))
                        {
                            ModEntry.ModMonitor.Log($"{args[1]}: successfully removed {characterName}", LogLevel.Info);
                        }
                        else
                        {
                            ModEntry.ModMonitor.Log($"{args[1]}: {characterName} not found, could not remove", LogLevel.Info);
                        }
                    }
                    break;
                case "hasseen":
                    foreach (string characterName in args[2..])
                    {
                        if(HasSeenDialogue(args[1], characterName))
                        {
                            ModEntry.ModMonitor.Log($"{args[1]} has character {characterName}", LogLevel.Info);
                        }
                        else
                        {
                            ModEntry.ModMonitor.Log($"{args[1]} does NOT have character {characterName}", LogLevel.Info);
                        }
                    }
                    break;
                default:
                    ModEntry.ModMonitor.Log($"{args[0]} is not a valid action (add/remove/hasseen)");
                    break;
            }
        }

        public static bool HasSeenDialogue(string key, string characterName)
        {
            if (!Context.IsWorldReady) { return false; }
            DialogueLog dialogueLog = ModEntry.DataHelper.ReadGlobalData<DialogueLog>($"{Constants.SaveFolderName}_{key}");
            if (dialogueLog == null) { return false; }
            return dialogueLog.Contains(characterName);
        }

        public static bool TryAddSeenDialogue(string key, string characterName)
        {
            if (!Context.IsWorldReady) { throw new SaveNotLoadedError(); }
            DialogueLog dialogueLog = ModEntry.DataHelper.ReadGlobalData<DialogueLog>($"{Constants.SaveFolderName}_{key}") ?? new DialogueLog(key, Constants.SaveFolderName);
            bool success = dialogueLog.Add(characterName);
            ModEntry.DataHelper.WriteGlobalData($"{Constants.SaveFolderName}_{key}", dialogueLog);
            return success;
        }

        public static bool TryRemoveSeenDialogue(string key, string characterName)
        {
            DialogueLog dialogueLog = ModEntry.DataHelper.ReadGlobalData<DialogueLog>($"{Constants.SaveFolderName}_{key}");
            if (dialogueLog == null) { return false; }
            return dialogueLog.Remove(characterName);

        }
        public static void PostfixCheckDialogue(ref bool __result, ref NPC __instance, int __0, bool __1)
        {
            try
            {
                if (__result) { return; } //have already found a New Current Dialogue
                foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
                {

                    string baseKey = ((__1) ? specialOrder.questKey.Value : Game1.currentSeason + specialOrder.questKey.Value);
                    baseKey += specialOrder.questState.Value switch
                    {
                        SpecialOrder.QuestState.InProgress => "_InProgress",
                        SpecialOrder.QuestState.Failed => "_Failed",
                        SpecialOrder.QuestState.Complete => "_Completed",
                        _ => throw new UnexpectedEnumValueException<SpecialOrder.QuestState>(specialOrder.questState.Value),
                    };

                    string dialogueKey = $"{baseKey}_{Game1.shortDayDisplayNameFromDayOfSeason(Game1.dayOfMonth)}";
                    if (__instance.Dialogue.ContainsKey(dialogueKey))
                    {
                        if (!TryAddSeenDialogue(dialogueKey, __instance.Name)) { continue; } //I have already said this dialogue
                        __instance.CurrentDialogue.Push(new Dialogue(__instance.Dialogue[dialogueKey], __instance) { removeOnNextMove = true });
                        ModEntry.ModMonitor.Log($"Found key {dialogueKey}", LogLevel.Trace);
                        __result = true;
                        return;
                    }

                    for (int heartLevel = 14; heartLevel > 0; heartLevel -= 2)
                    {
                        dialogueKey = $"{baseKey}{heartLevel}";
                        if (__0 > heartLevel && __instance.Dialogue.ContainsKey(dialogueKey))
                        {
                            if (!TryAddSeenDialogue(dialogueKey, __instance.Name)) { continue; } //I have already said this dialogue
                            __instance.CurrentDialogue.Push(new Dialogue(__instance.Dialogue[dialogueKey], __instance) { removeOnNextMove = true });
                            ModEntry.ModMonitor.Log($"Found key {dialogueKey}", LogLevel.Trace);
                            __result = true;
                            return;
                        }
                    }

                    if (__instance.Dialogue.ContainsKey(baseKey))
                    {
                        if (!TryAddSeenDialogue(baseKey, __instance.Name)) { continue; } //I have already said this dialogue
                        __instance.CurrentDialogue.Push(new Dialogue(__instance.Dialogue[baseKey], __instance) { removeOnNextMove = true });
                        ModEntry.ModMonitor.Log($"Found key {baseKey}", LogLevel.Trace);
                        __result = true;
                        return;
                    }

                    ModEntry.ModMonitor.Log($"Did not find dialogue key for special order {baseKey} for NPC {__instance.Name}", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Failed in checking for Special Order dialogue for NPC {__instance.Name}\n{ex}", LogLevel.Error);
            }
        }
    }

    internal class DialogueLog
    {
        readonly string key;
        readonly string savefile;
        public List<string> CharactersList { get; set; } = new();

        public DialogueLog(string key, string savefile)
        {
            this.key = key;
            this.savefile = savefile;
        }

        public bool Contains(string characterName)
        {
            return CharactersList.Contains(characterName);
        }

        public bool Add(string characterName)
        {
            if (CharactersList.Contains(characterName)) { return false; }
            else { CharactersList.Add(characterName); return true; }
        }

        public bool Remove(string characterName)
        {
            return CharactersList.Remove(characterName);
        }
        public override string ToString()
        {
            return $"DialogueLog({savefile}:{key}): {String.Join(", ", CharactersList)}";
        }
    }

}
