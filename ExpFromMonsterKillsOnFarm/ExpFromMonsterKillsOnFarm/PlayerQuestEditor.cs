using Netcode;

using StardewValley.Quests;

namespace ExpFromMonsterKillsOnFarm;
internal sealed class PlayerQuestEditor
{
    internal void OnElementChanged(NetList<Quest, NetRef<Quest>> list, int index, Quest oldValue, Quest newValue)
    {
        if (newValue is SlayMonsterQuest slay)
        {
            slay.ignoreFarmMonsters.Value = false;
        }
    }

    internal void OnArrayReplaced(NetList<Quest, NetRef<Quest>> list, IList<Quest> before, IList<Quest> after)
    {
        if (after is null)
        {
            return;
        }

        foreach (var quest in after)
        {
            if (quest is SlayMonsterQuest slay)
            {
                slay.ignoreFarmMonsters.Value = false;
            }
        }
    }
}
