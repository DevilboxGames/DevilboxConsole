
using DevilboxConsole.examples.Shared.scripts.interfaces;
using Godot;

namespace DevilboxConsole.examples.Shared.scripts;

public partial class ExamplePlayerController : Node
{
    [ExportGroup("Nodes")]
    [ExportSubgroup("Controllers")]
    [Export]
    public FPSMoveController MoveController { get; set; }
    [Export]
    public FPSMouseAimController MouseAimController { get; set; }
    
    [ExportSubgroup("General Nodes")]
    [Export]
    public CharacterBody3D Character { get; set; }
    [Export]
    public RayCast3D ActionCast { get; set; }
    [Export]
    public Camera3D Camera { get; set; }
    
    [ExportGroup("Action Settings")]
    [Export]
    public float ActionDistance { get; set; }

    [ExportGroup("Action Names")] 
    [Export]
    public StringName PerformActionAction { get; set; } = "perform_action";
    
    StringName uiCancel = new StringName("ui_cancel");
    public bool InputEnabled { get; set; } = true;
    public override void _PhysicsProcess(double delta)
    {
        if (!InputEnabled)
        {
            return;
        }
        
        if (Input.IsActionJustPressed(PerformActionAction) && ActionCast.IsColliding())
        {
            var collidingObject = ActionCast.GetCollider();

            if (collidingObject is IInteractableObject interactableObject && interactableObject.IsInteractable())
            {
                interactableObject.Interact(this);
            }
        }

    }

    public override void _UnhandledInput(InputEvent @event)
    {
        
        if (@event is InputEventKey keyEvent && keyEvent.IsActionPressed(uiCancel))
        {
            if (Input.MouseMode != Input.MouseModeEnum.Captured)
            {
                GetTree().Quit();
            }
            else
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }

            return;
        }
    }

    public void DisableControllers()
    {
        InputEnabled = false;
        MoveController.InputEnabled = false;
        MouseAimController.InputEnabled = false;
        GD.PrintRich("[color=red]Player Controllers Disabled[/color]");
    }
    public void EnableControllers()
    {
        InputEnabled = true;
        MoveController.InputEnabled = true;
        MouseAimController.InputEnabled = true;
        GD.PrintRich("[color=green]Player Controllers Enabled[/color]");
    }
}