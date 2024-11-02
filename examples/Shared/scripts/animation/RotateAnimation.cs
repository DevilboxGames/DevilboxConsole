using DevilboxConsole.examples.Shared.scripts.interfaces;
using Godot;

namespace DevilboxConsole.examples.Shared.scripts.animation;

public partial class RotateAnimation : Node, INodeAnimation
{
    [Export]
    public bool IsPlaying { get; set; }
    [Export]
    public bool AutoPlay { get; set; }

    [Export]
    public Vector3 Axis { get; set; } = Vector3.Up;
    [Export]
    public float StartAngle { get; set; } = 0;
    
    [Export]
    public float TargetAngle { get; set; } =90;
    
    [Export]
    public float RotateTime { get; set; } = 1;
    
    [Export]
    public Curve ScaleCurve { get; set; } = new Curve(){ _Data = {0, 1}};

    
    protected Node3D ParentNode3D { get => _parentNode as Node3D; }
    protected Node2D ParentNode2D { get => _parentNode as Node2D; }
    protected Control ParentControl { get => _parentNode as Control; }
    private Node _parentNode;

    private double _progress;

    protected void SetParentRotation(float angle)
    {
        GD.Print($"SetRotation on type {_parentNode.GetType().Name}: {angle}");
        if (ParentNode3D != null)
        {
            ParentNode3D.Rotation = Axis * Mathf.DegToRad(angle);
        }
        else if(ParentNode2D != null)
        {
            ParentNode2D.Rotation = Mathf.DegToRad(angle);
        }
        else if (ParentControl != null)
        {
            ParentControl.Rotation = Mathf.DegToRad(angle);
        }
    }
    public override void _Ready()
    {
        _parentNode = GetParent();
        SetParentRotation(StartAngle);
        if (AutoPlay)
        {
            Play();
        }
    }

    public override void _Process(double delta)
    {
        if (IsPlaying && _progress < RotateTime)
        {
            GD.Print($"Playing rotation: {_progress}");
            _progress += delta;
            double progressFraction = Mathf.Min(1, _progress / RotateTime);
            float computedProgress = ScaleCurve.Sample((float)progressFraction);
            float newAngle = Mathf.Lerp(StartAngle, TargetAngle, computedProgress);
            
            SetParentRotation(newAngle);
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
        Reset();
        IsPlaying = true;
        
    }

    public void Reset()
    {
        _progress = 0;
        SetParentRotation(StartAngle);
    }

    public void SkipToEnd()
    {
        _progress = RotateTime;
        SetParentRotation(TargetAngle);
        IsPlaying = false;
    }
}