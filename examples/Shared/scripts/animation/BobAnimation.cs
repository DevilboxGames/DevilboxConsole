using DevilboxConsole.examples.Shared.scripts.interfaces;
using Godot;

namespace DevilboxConsole.examples.Shared.scripts.animation;

public partial class BobAnimation : Node, INodeAnimation
{
    [Export]
    public bool IsPlaying { get; set; }

    [Export]
    public bool AutoPlay { get; set; } = true;
    [Export]
    public float BaseHeight { get; set; } = 2;
    [Export]
    public Vector3 BobVector { get; set; } = Vector3.Up;

    [Export]
    public float BobHeight { get; set; } = 1;

    [Export]
    public float BobSpeed { get; set; } = 1;

    private Node3D _parentNode { get; set; }

    private double _period { get; set; }
    public override void _Ready()
    {
        _parentNode = GetParent() as Node3D;
        if (AutoPlay)
        {
            Play();
        }
    }

    public override void _Process(double delta)
    {
        if (IsPlaying)
        {
            _period += delta;

            double newHeight = Mathf.Sin(_period * BobSpeed) * BobHeight + BaseHeight;

            _parentNode.Position = BobVector * (float)newHeight;
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
        _parentNode.Position = BobVector * BaseHeight;
        _period = 0;
    }
}