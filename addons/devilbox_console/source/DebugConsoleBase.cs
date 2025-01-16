using Godot;

namespace DevilboxGames.DebugConsole;

public partial class DebugConsoleBase : CommandConsole
{

    [Export(PropertyHint.Range, "0.2:1")]
    public float ConsoleHeight
    {
        get => _consoleHeight;
        set
        {
            _consoleHeight = value;
            
            if (Console != null && !Console.IsQueuedForDeletion() && GodotObject.IsInstanceValid(Console))
            {
                DebugConsole.ConsoleHeight = _consoleHeight;
            }
        }
    }
    public DebugConsole DebugConsole => Console as DebugConsole;
    protected override string ConsoleScene { get; set; } = "res://addons/devilbox_console/scenes/DebugConsoleScene.tscn";

    private float _consoleHeight = 1.0f;
    public override void _EnterTree()
    {
        base._EnterTree();
        
        DebugConsole.ConsoleHeight = ConsoleHeight;
    }
}