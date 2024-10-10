using DevilboxConsole.examples.Shared.scripts.hud;
using Godot;

namespace DevilboxConsole.examples.Shared.scripts;

public partial class ExamplePlayer : CharacterBody3D
{
    [Export]
    public ExampleHudBase Hud { get; set; }
    [Export]
    public Node DialogueSequenceNode { get; set; }
    [Export]
    public float Health
    {
        get=>_health;
        set
        {
            _health = value;
            if (Hud != null)
            {
                Hud.Health = value;
            }
        }
    }

    [Export]
    public int Ammo
    {
        get=> _ammo;
        set
        {
            _ammo = value;
            if (Hud != null)
            {
                Hud.Ammo = value;
            }
        }
    }

    private float _health;
    private int _ammo;

    public override void _Ready()
    {
        Health = _health;
        Ammo = _ammo;
        base._Ready();
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey eventKey && eventKey.Pressed && eventKey.Keycode == Key.F1)
        {
            (DialogueSequenceNode as DialogueSequenceBox)?.ShowDialogueSequence();
        }
    }
}