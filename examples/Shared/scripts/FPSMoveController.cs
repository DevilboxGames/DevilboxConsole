using DevilboxConsole.examples.Shared.scripts.singletons;
using Godot;

namespace DevilboxConsole.examples.Shared.scripts;

public partial class FPSMoveController : Node
{
    [ExportGroup("Nodes")]
    [Export]
    public CharacterBody3D Character { get; set; }
    
    [ExportGroup("Settings")] 
    [Export] 
    public float WalkSpeed { get; set; } = 10;
    
    [Export] 
    public float RunSpeed { get; set; } = 30;
    
    [Export] 
    public float JumpForce { get; set; } = 30;
    [Export] 
    public float Gravity { get; set; } = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    [Export]
    public Vector3 GravityDirection { get; set; } = Vector3.Down;
    public bool InputEnabled { get; set; } = true;

    [ExportGroup("Action Names")] 
    [Export]
    public StringName ForwardAction { get; set; } = "move_forward";
    [Export]
    public StringName BackwardAction { get; set; } = "move_back";
    [Export]
    public StringName LeftAction { get; set; } = "move_left";
    [Export]
    public StringName RightAction { get; set; } = "move_right";
    [Export]
    public StringName JumpAction { get; set; } = "jump";

    public override void _PhysicsProcess(double delta)
    {
        Vector2 movement = GameStateUtil.PlayerInputState == GameStateUtil.PlayerInputStateMode.Controller ? Input.GetVector(LeftAction, RightAction, ForwardAction, BackwardAction) : Vector2.Zero;
        Vector3 direction = (Character.Transform.Basis * new Vector3(movement.X, 0, movement.Y)).Normalized();
        
        Vector3 velocity = Character.Velocity;

        
        if (!Character.IsOnFloor())
        {

            velocity.X = Mathf.Lerp(velocity.X, direction.X * WalkSpeed, (float)delta * 0.01f);
            velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * WalkSpeed, (float)delta * 0.01f);
            
            velocity += Gravity * GravityDirection * (float)delta;
        }
        else
        {
            
            if (!Mathf.IsZeroApprox(movement.X) || !Mathf.IsZeroApprox(movement.Y))
            {
                velocity = direction * WalkSpeed;
            }
            else
            {
                velocity.X = Mathf.Lerp(velocity.X, direction.X * WalkSpeed, (float)delta * 7.0f);
                velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * WalkSpeed, (float)delta * 7.0f);
            }
            if (GameStateUtil.PlayerInputState == GameStateUtil.PlayerInputStateMode.Controller && Input.IsActionJustPressed(JumpAction))
            {
                velocity += -GravityDirection * JumpForce;
            }
        }

        if (Character.Transform.Basis.Y != Character.UpDirection)
        {
            var newForward = Character.UpDirection.Cross(Character.Basis.X).Normalized();
            var newBasis = Basis.LookingAt(newForward, Character.UpDirection);
            Character.Basis = Character.Basis.Slerp(newBasis, (float)delta * 7.0f).Orthonormalized();
        }

        Character.Velocity = velocity;

        Character.MoveAndSlide();
    }
}