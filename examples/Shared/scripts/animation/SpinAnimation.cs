using DevilboxConsole.examples.Shared.scripts.interfaces;
using Godot;

namespace DevilboxConsole.examples.Shared.scripts.animation;

public partial class SpinAnimation : Node, INodeAnimation
{
    [Export]
    public bool IsPlaying { get; set; }
    [Export]
    public bool AutoPlay { get; set; }
    [Export]
    public float BaseAngle { get; set; } = 0;

    [Export]
    public Vector3 SpinAxis { get; set; } = Vector3.Up;

    [Export]
    public float SpinSpeed { get; set; } = 180;

    [Export]
    public bool FlipDirection { get; set; }

    private Node3D _parentNode { get; set; }


    public override void _Ready()
    {
        _parentNode = GetParent() as Node3D;
        _parentNode.Quaternion = new Quaternion(SpinAxis, Mathf.DegToRad(BaseAngle));
        if (AutoPlay)
        {
            Play();
        }
    }

    public override void _Process(double delta)
    {
        if (IsPlaying)
        {
            _parentNode.Rotate(SpinAxis, Mathf.DegToRad((float)delta * SpinSpeed * (FlipDirection ? -1 : 1)));
        }
    }
    public void Play()
    {
        IsPlaying = true;
    }

    public void Stop()
    {
        IsPlaying = false;
    }

    public void Restart()
    {
        IsPlaying = true;
        _parentNode.Quaternion = new Quaternion(SpinAxis, Mathf.DegToRad(BaseAngle));
    }
}