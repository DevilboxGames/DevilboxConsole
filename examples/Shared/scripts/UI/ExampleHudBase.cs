using Godot;

namespace DevilboxConsole.examples.Shared.scripts.hud;

public partial class ExampleHudBase : Node
{
    private float _health;
    private int _ammo;

    [Export]
    public ProgressBar HealthBar { get; set; }
	
    [Export]
    public RichTextLabel AmmoCount { get; set; }

    [Export]
    public float Health
    {
        get => _health;
        set
        {
            _health = value;
            HealthBar?.SetValue(_health);
        }
    }

    [Export]
    public int Ammo
    {
        get => _ammo;
        set
        {
            _ammo = value;
            if (AmmoCount != null)
            {
                AmmoCount.Text = _ammo.ToString();
            }
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Ammo = _ammo;
        Health = _health;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}