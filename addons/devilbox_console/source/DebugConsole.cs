using System;
using Godot;
using RogueDragon.Scripts.Utility;

namespace DevilboxGames.DebugConsole;

public partial class DebugConsole : ConsoleControl
{
   
    [Export]
    public float ConsoleHeight { get=> AnchorBottom; set=> AnchorBottom = value; }
    public override string DefaultOutput { get; set; } =
        "Welcome to Devilbox Debug Console [color=red]v{{DebugConsole.Version}}[/color]";
    public bool IsShowing { get; protected set; }
    
    public event Action OnShowConsole;
    public event Action OnHideConsole;
    public override void _Ready()
    {
        Hide();
        ProcessMode = ProcessModeEnum.Always;
        IsShowing = false;
        base._Ready();
    }

    public void ShowConsole()
    {
        IsShowing = true;
        IsFocused = true;
        this.TakeInputControl();
        //GetTree().SetPause(true);
        Show();
        OnShowConsole?.Invoke();
    }
    public void HideConsole()
    {
        IsShowing = false;
        IsFocused = false;
        this.ReleaseInputControl();
        //GetTree().SetPause(false);
        Hide();
        OnHideConsole?.Invoke();
    }

    public void Toggle()
    {
        if (IsShowing)
        {
            HideConsole();
        }
        else
        {
            ShowConsole();
        }
    }

    public override void HandleInput(InputEvent @event)
    {
        //GD.PrintRich($"[color=green]DebugConsole \"{ConsoleName}\" HandleInput[/color]");
        if (@event is InputEventKey keyEvent)
        {
            if (keyEvent.Pressed)
            {
                if (keyEvent.Keycode == Key.Tab && keyEvent.ShiftPressed)
                {
                    Toggle();
                }
                else if (IsShowing)
                {
                    GD.Print("Is Showing");
                    HandleKeyInput(keyEvent);
                }
            }
        }
    }

    
}