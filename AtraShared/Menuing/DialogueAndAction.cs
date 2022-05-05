using StardewValley.Menus;

namespace AtraShared.Menuing;

/// <summary>
/// Shamelessly stolen from RSV: https://github.com/Rafseazz/Ridgeside-Village-Mod/blob/main/Ridgeside%20SMAPI%20Component%202.0/RidgesideVillage/DialogueMenu.cs.
/// </summary>
internal class DialogueAndAction : DialogueBox
{

    private List<Action?> actions;

    internal DialogueAndAction(string dialogue, List<Response> responses, List<Action?> actions)
        : base(dialogue, responses)
    {
        this.actions = actions;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        int responseIndex = this.selectedResponse;
        base.receiveLeftClick(x, y, playSound);
        if (this.safetyTimer <= 0 && responseIndex > -1 && responseIndex < this.actions.Count)
        {
            this.actions[responseIndex]?.Invoke();
        }
    }
}