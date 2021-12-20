using System;
using System.Collections.Generic;
using System.Text;

using StardewModdingAPI;

using StardewValley;
using SpecialOrdersExtended.DataModels;

namespace SpecialOrdersExtended
{

    internal class DialogueManager
    {
        private static DialogueLog DialogueLog;
        public static void LoadDialogueLog() => DialogueLog = DialogueLog.Load();

        public static void SaveDialogueLog() => DialogueLog.Save();

        public static void ConsoleSpecialOrderDialogue(string command, string[] args)
        {
            if (!Context.IsWorldReady)
            {
                ModEntry.ModMonitor.Log(I18n.LoadSaveFirst(), LogLevel.Warn);
            }
            if (args.Length < 3)
            {
                ModEntry.ModMonitor.Log(I18n.Dialogue_ConsoleError(command), LogLevel.Warn);
                return;
            }
            switch (args[0])
            {
                case "add":
                    foreach (string characterName in args[2..])
                    {
                        if(TryAddSeenDialogue(args[1], characterName))
                        {
                            ModEntry.ModMonitor.Log(I18n.Dialogue_ConsoleAddSuccess(args[1], characterName), LogLevel.Info);
                        }
                        else
                        {
                            ModEntry.ModMonitor.Log(I18n.Dialogue_ConsoleAddFailure(args[1], characterName), LogLevel.Info);
                        }
                    }
                    break;
                case "remove":
                    foreach (string characterName in args[2..])
                    {
                        if(TryRemoveSeenDialogue(args[1], characterName))
                        {
                            ModEntry.ModMonitor.Log(I18n.Dialogue_ConsoleRemoveSuccess(args[1], characterName), LogLevel.Info);
                        }
                        else
                        {
                            ModEntry.ModMonitor.Log(I18n.Dialogue_ConsoleRemoveFailure(args[1], characterName), LogLevel.Info);
                        }
                    }
                    break;
                case "hasseen":
                    foreach (string characterName in args[2..])
                    {
                        if(HasSeenDialogue(args[1], characterName))
                        {
                            ModEntry.ModMonitor.Log(I18n.Dialogue_ConsoleDoesContain(args[1], characterName), LogLevel.Info);
                        }
                        else
                        {
                            ModEntry.ModMonitor.Log(I18n.Dialogue_ConsoleDoesNotContain(args[1], characterName), LogLevel.Info);
                        }
                    }
                    break;
                default:
                    ModEntry.ModMonitor.Log(I18n.Dialogue_ConsoleActionInvalid(args[0]), LogLevel.Info);
                    break;
            }
        }

        public static bool HasSeenDialogue(string key, string characterName)
        {
            if (!Context.IsWorldReady) { throw new SaveNotLoadedError(); }
            return DialogueLog.Contains(key, characterName);
        }

        public static bool TryAddSeenDialogue(string key, string characterName)
        {
            if (!Context.IsWorldReady) { throw new SaveNotLoadedError(); }
            return DialogueLog.Add(key, characterName);
        }

        public static bool TryRemoveSeenDialogue(string key, string characterName)
        {
            if (!Context.IsWorldReady) { throw new SaveNotLoadedError(); }
            return DialogueLog.Remove(key, characterName);
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
                        ModEntry.ModMonitor.Log(I18n.Dialogue_FoundKey(dialogueKey), LogLevel.Trace);
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
                            ModEntry.ModMonitor.Log(I18n.Dialogue_FoundKey(dialogueKey), LogLevel.Trace);
                            __result = true;
                            return;
                        }
                    }

                    if (__instance.Dialogue.ContainsKey(baseKey))
                    {
                        if (!TryAddSeenDialogue(baseKey, __instance.Name)) { continue; } //I have already said this dialogue
                        __instance.CurrentDialogue.Push(new Dialogue(__instance.Dialogue[baseKey], __instance) { removeOnNextMove = true });
                        ModEntry.ModMonitor.Log(I18n.Dialogue_FoundKey(baseKey), LogLevel.Trace);
                        __result = true;
                        return;
                    }

                    ModEntry.ModMonitor.Log(I18n.Dialogue_NoKey(baseKey, __instance.Name), LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"{I18n.Dialogue_ErrorInPatchedFunction(__instance.Name)}\n{ex}", LogLevel.Error);
            }
        }
    }



}
