using DevilboxConsole.examples.Shared.scripts.singletons;
using Godot;

namespace DevilboxConsole.examples.Shared.scripts;

public partial class FPSMouseAimController : Node
{
    [ExportGroup("Nodes")]
    [Export]
    public CharacterBody3D Character { get; set; }
    
    [Export]
    public Node3D Head { get; set; }

    [ExportGroup("Settings")]
    [ExportSubgroup("Mouse Settings")]
    [Export(PropertyHint.Range, "1,100,1")]
    public int HorizontalSensitivity { get; set; } = 50;
    
    [Export(PropertyHint.Range, "1,100,1")]
    public int VerticalSensitivity { get; set; } = 50;
    [Export]
    public float DegreesPerUnit { get; set; } = 0.001f;

    [ExportSubgroup("Clamp Settings")]
    [Export]
    public float MinPitch
    {
        get => _minPitch;
        set
        {
            _minPitch = value;
            _minPitchRad = Mathf.DegToRad(value);
        }
    }

    [Export]
    public float MaxPitch
    {
        get => _maxPitch;
        set
        {
            _maxPitch = value;
            _maxPitch = Mathf.DegToRad(value);
        }
    }

    [ExportGroup("State")]
    public bool InputEnabled { get; set; } = true;
    public Vector3 YawVector = Vector3.Down;
    public Vector3 PitchVector = Vector3.Left;
    private float _maxPitch = 89;
    private float _maxPitchRad = Mathf.DegToRad(89);
    private float _minPitch = -89;
    private float _minPitchRad = Mathf.DegToRad(-89);


    public override void _Ready()
    {
        Input.UseAccumulatedInput = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (GameStateUtil.PlayerInputState != GameStateUtil.PlayerInputStateMode.Controller)
        {
            return;
        }
        if (Input.MouseMode != Input.MouseModeEnum.Captured && @event is InputEventMouseButton mouseButtonEvent &&
            mouseButtonEvent.ButtonIndex == MouseButton.Left)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
        else if (Input.MouseMode == Input.MouseModeEnum.Captured && @event is InputEventMouseMotion motionEvent)
        {
            AimLook(motionEvent.Relative);
        }
        

    }

    private void AimLook(Vector2 motion)
    {
        motion.X *= HorizontalSensitivity * DegreesPerUnit;
        motion.Y *= HorizontalSensitivity * DegreesPerUnit;

        if (!Mathf.IsZeroApprox(motion.X))
        {
            Character.RotateObjectLocal(YawVector, Mathf.DegToRad(motion.X));
        }

        if (!Mathf.IsZeroApprox(motion.Y))
        {
            Head.RotateObjectLocal(PitchVector, Mathf.DegToRad(motion.Y));

            if (Head.Rotation.X < _minPitchRad || Head.Rotation.X > _maxPitchRad)
            {
                Head.Rotation = Head.Rotation with { X = Mathf.Clamp(Head.Rotation.X, _minPitchRad, _maxPitchRad) };
                Head.Orthonormalize();
            }
        }
    }
}