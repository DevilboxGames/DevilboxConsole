using DevilboxConsole.examples.Shared.scripts.interfaces;
using Godot;
using Godot.Collections;

namespace DevilboxConsole.examples.Shared.scripts.animation;

public partial class ScaleAnimation : Node, INodeAnimation
{
    [Export]
    public bool IsPlaying { get; set; }
    [Export]
    public bool AutoPlay { get; set; }

    [Export]
    public float StartScale { get; set; } = 1;
    
    [Export]
    public float TargetScale { get; set; } =2;
    
    [Export]
    public float ScaleTime { get; set; } = 1;
    
    [Export]
    public Curve ScaleCurve { get; set; } = new Curve(){ _Data = {0, 1}};

    
    protected Node3D ParentNode3D { get => _parentNode as Node3D; }
    protected Node2D ParentNode2D { get => _parentNode as Node2D; }
    protected Control ParentControl { get => _parentNode as Control; }
    private Node _parentNode;

    private double _progress;

    protected void SetParentScale(float scale)
    {
        
        if (ParentNode3D != null)
        {
            ParentNode3D.Scale = new Vector3(scale,scale,scale);
        }
        else if(ParentNode2D != null)
        {
            ParentNode2D.Scale = new Vector2(scale, scale);
        }
        else if (ParentControl != null)
        {
            ParentControl.Scale = new Vector2(scale, scale);
        }
    }
    public override void _Ready()
    {
        _parentNode = GetParent() as Node3D;
        SetParentScale(StartScale);
        if (AutoPlay)
        {
            Play();
        }
    }

    public override void _Process(double delta)
    {
        if (IsPlaying && _progress < ScaleTime)
        {
            _progress += delta;
            double progressFraction = Mathf.Min(1, _progress / ScaleTime);
            float computedProgress = ScaleCurve.Sample((float)progressFraction);
            float newScale = Mathf.Lerp(StartScale, TargetScale, computedProgress);
            
            SetParentScale(newScale);
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
        SetParentScale(StartScale);
    }

    public void SkipToEnd()
    {
        IsPlaying = false;
        _progress = ScaleTime;
        SetParentScale(TargetScale);
    }
}