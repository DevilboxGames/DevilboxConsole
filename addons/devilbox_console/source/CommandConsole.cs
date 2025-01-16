using Godot;

namespace DevilboxGames.DebugConsole;

public partial class CommandConsole : Node
{
    [Export]
    public string ConsoleName
    {
        get => _consoleName;
        set
        {
            _consoleName = value;
            if (Console != null)
            {
                Console.ConsoleName = _consoleName;
            }
        }
    }

    [Export]
    public bool LimitCommands { get; set; }
    [Export]
    public ConsoleControl Console { get; set; }
    [Export]
    public string DefaultOutput { get; set; }
    
    protected virtual string ConsoleScene { get; set; } = "res://addons/devilbox_console/scenes/CommandConsoleScene.tscn";
    
    private string _consoleName;
    public override void _EnterTree()
    {
        var scene = GD.Load<PackedScene>(ConsoleScene);
        Console = scene.Instantiate() as ConsoleControl;
        Console.ConsoleName = ConsoleName;
        Console.LimitCommands = LimitCommands;

        if (!string.IsNullOrWhiteSpace(DefaultOutput))
        {
            Console.DefaultOutput = DefaultOutput;
        }
        AddChild(Console);
    }   
}