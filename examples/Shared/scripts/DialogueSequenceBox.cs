using DevilboxConsole.examples.Shared.scripts.hud;
using Godot;
using Godot.Collections;

namespace DevilboxConsole.examples.Shared.scripts;

public partial class DialogueSequenceBox : Node
{
    [Export(PropertyHint.MultilineText)]
    public Array<string> DialogueSequence { get; set; }
    [Export]
    public StringName ProgressActionName { get; set; }
    [Export]
    public Node MessageBoxNode { get; set; }
    protected MessageBox MessageBox{get=> MessageBoxNode as MessageBox;}

    protected int messageIndex = 0;



    public void ShowDialogueSequence()
    {
        MessageBox.ShowMessage();
        messageIndex = 0;
        MessageBox.Message = DialogueSequence[messageIndex].Replace("\\n", "\n");
    }

    public void NextMessage()
    {
        messageIndex++;
        if (messageIndex >= DialogueSequence.Count)
        {
            MessageBox.CloseMessage();
        }
        else
        {
            MessageBox.Message = DialogueSequence[messageIndex].Replace("\\n", "\n");
        }
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey eventKey && eventKey.Pressed && eventKey.Keycode == Key.Enter)
        {
            NextMessage();
        }
    }
}