using Microsoft.Xna.Framework.Input;
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

    public override void receiveKeyPress(Keys key)
    {
        base.receiveKeyPress(key);
        if (this.safetyTimer > 0)
        {
            return;
        }
        for (int i = 0; i < this.responses.Count; i++)
        {
            if (this.responses[i].hotkey == key)
            {
                if (i < this.actions.Count)
                {
                    this.actions[i]?.Invoke();
                    this.closeDialogue();
                }
            }
        }
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (this.safetyTimer <= 0 && this.selectedResponse > -1 && this.selectedResponse < this.actions.Count)
        {
            this.actions[this.selectedResponse]?.Invoke();
        }
        base.receiveLeftClick(x, y, playSound);
    }
}