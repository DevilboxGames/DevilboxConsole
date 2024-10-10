

using DevilboxGames.DebugConsole;
using Godot;

namespace DevilboxConsole.examples.Shared.scripts;

public partial class DisableControllerOnConsoleOpen : Node
{
    [Export]
    public ExamplePlayerController PlayerController { get; set; }
    
    DebugConsole _debugConsole;
    public override void _Ready()
    {
        _debugConsole = GetNode<DebugConsole>("/root/DebugConsole");
        _debugConsole.OnShowConsole += OnShowConsole;
        _debugConsole.OnHideConsole += OnHideConsole;
    }

    private void OnHideConsole()
    {
        PlayerController?.EnableControllers();
    }

    private void OnShowConsole()
    {
        PlayerController?.DisableControllers();
    }

    public override void _ExitTree()
    {
        _debugConsole.OnShowConsole -= OnShowConsole;
        _debugConsole.OnHideConsole -= OnHideConsole;
    }
}